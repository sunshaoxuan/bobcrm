using System;
using System.Collections.Generic;
using System.Threading;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BobCrm.Api.Services;

public interface IDefaultTemplateService
{
    Task EnsureSystemTemplateAsync(EntityDefinition entityDefinition, string? publishedBy, CancellationToken ct = default);
}

public class DefaultTemplateService : IDefaultTemplateService
{
    private readonly AppDbContext _db;
    private readonly IDefaultTemplateGenerator _generator;
    private readonly TemplateBindingService _bindingService;
    private readonly ILogger<DefaultTemplateService> _logger;

    public DefaultTemplateService(
        AppDbContext db,
        IDefaultTemplateGenerator generator,
        TemplateBindingService bindingService,
        ILogger<DefaultTemplateService> logger)
    {
        _db = db;
        _generator = generator;
        _bindingService = bindingService;
        _logger = logger;
    }

    public async Task EnsureSystemTemplateAsync(EntityDefinition entityDefinition, string? publishedBy, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entityDefinition);

        var result = _generator.Generate(entityDefinition);
        var templateModel = result.Template;
        templateModel.EntityType ??= entityDefinition.EntityRoute;

        var existing = await _db.FormTemplates
            .FirstOrDefaultAsync(t => t.EntityType == templateModel.EntityType
                                      && t.IsSystemDefault
                                      && t.UsageType == templateModel.UsageType, ct);

        if (existing == null)
        {
            _db.FormTemplates.Add(templateModel);
            await _db.SaveChangesAsync(ct);
            existing = templateModel;
            _logger.LogInformation("Created system default template {TemplateId} for {EntityType}", existing.Id, existing.EntityType);
        }
        else
        {
            existing.Name = templateModel.Name;
            existing.LayoutJson = templateModel.LayoutJson;
            existing.Description = templateModel.Description;
            existing.UserId = templateModel.UserId;
            existing.Tags = templateModel.Tags is null ? null : new List<string>(templateModel.Tags);
            existing.IsSystemDefault = true;
            existing.IsUserDefault = false;
            existing.IsInUse = true;
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Updated system default template {TemplateId} for {EntityType}", existing.Id, existing.EntityType);
        }

        await _bindingService.UpsertBindingAsync(
            templateModel.EntityType!,
            templateModel.UsageType,
            existing.Id,
            isSystem: true,
            updatedBy: publishedBy ?? "system",
            requiredFunctionCode: templateModel.RequiredFunctionCode,
            ct: ct);
    }
}
