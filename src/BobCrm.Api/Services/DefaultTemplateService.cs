using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Application.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BobCrm.Api.Services;

public interface IDefaultTemplateService
{
    Task<DefaultTemplateGenerationResult> EnsureTemplatesAsync(
        EntityDefinition entityDefinition,
        string? updatedBy,
        CancellationToken ct = default);

    Task<FormTemplate> GetDefaultTemplateAsync(
        EntityDefinition entityDefinition,
        FormTemplateUsageType usageType,
        string? requestedBy = null,
        CancellationToken ct = default);
}

public class DefaultTemplateService : IDefaultTemplateService
{
    private readonly AppDbContext _db;
    private readonly IDefaultTemplateGenerator _generator;
    private readonly ILogger<DefaultTemplateService> _logger;

    public DefaultTemplateService(
        AppDbContext db,
        IDefaultTemplateGenerator generator,
        ILogger<DefaultTemplateService> logger)
    {
        _db = db;
        _generator = generator;
        _logger = logger;
    }

    public async Task<DefaultTemplateGenerationResult> EnsureTemplatesAsync(
        EntityDefinition entityDefinition,
        string? updatedBy,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entityDefinition);

        var result = await _generator.EnsureTemplatesAsync(entityDefinition, ct);

        if (result.Created.Count > 0 || result.Updated.Count > 0)
        {
            _logger.LogInformation(
                "Ensured default templates for {EntityType} by {User}. Created: {CreatedCount}, Updated: {UpdatedCount}",
                entityDefinition.EntityRoute ?? entityDefinition.EntityName,
                updatedBy ?? "system",
                result.Created.Count,
                result.Updated.Count);
        }

        return result;
    }

    public async Task<FormTemplate> GetDefaultTemplateAsync(
        EntityDefinition entityDefinition,
        FormTemplateUsageType usageType,
        string? requestedBy = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entityDefinition);

        var entityType = entityDefinition.EntityRoute
                         ?? throw new InvalidOperationException("EntityDefinition must specify EntityRoute");

        var template = await _db.FormTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(
                t => t.EntityType == entityType &&
                     t.IsSystemDefault &&
                     t.UsageType == usageType,
                ct);

        if (template != null)
        {
            return template;
        }

        var generated = await _generator.GenerateAsync(entityDefinition, usageType, ct);
        generated.EntityType ??= entityType;

        _db.FormTemplates.Add(generated);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Generated default template {TemplateId} for {EntityType} ({UsageType}) via GetDefaultTemplateAsync requested by {RequestedBy}",
            generated.Id,
            entityType,
            usageType,
            requestedBy ?? "system");

        return generated;
    }
}
