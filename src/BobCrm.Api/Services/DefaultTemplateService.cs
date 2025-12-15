using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Application.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BobCrm.Api.Services;

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
        bool force = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entityDefinition);

        var result = await _generator.EnsureTemplatesAsync(entityDefinition, force: force, ct);

        var entityType = entityDefinition.EntityRoute ?? entityDefinition.EntityName ?? string.Empty;
        var now = DateTime.UtcNow;
        foreach (var kvp in result.Templates)
        {
            var usage = MapViewStateToUsage(kvp.Key);
            var binding = await _db.TemplateBindings
                .FirstOrDefaultAsync(b => b.EntityType == entityType && b.UsageType == usage && b.IsSystem, ct);

            if (binding == null)
            {
                binding = new TemplateBinding
                {
                    EntityType = entityType,
                    UsageType = usage,
                    IsSystem = true,
                    TemplateId = kvp.Value.Id,
                    UpdatedAt = now,
                    UpdatedBy = updatedBy ?? "system",
                    RequiredFunctionCode = kvp.Value.RequiredFunctionCode
                };
                _db.TemplateBindings.Add(binding);
            }
            else
            {
                binding.TemplateId = kvp.Value.Id;
                binding.RequiredFunctionCode ??= kvp.Value.RequiredFunctionCode;
                binding.UpdatedAt = now;
                binding.UpdatedBy = updatedBy ?? "system";
                _db.TemplateBindings.Update(binding);
            }
        }

        if (result.Templates.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
        }

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

        var viewState = MapUsageToViewState(usageType);
        var entityType = entityDefinition.EntityRoute
                         ?? throw new InvalidOperationException("EntityDefinition must specify EntityRoute");

        // 查询通过 TemplateStateBinding 关联的默认模板
        var binding = await _db.TemplateStateBindings
            .AsNoTracking()
            .Include(b => b.Template)
            .FirstOrDefaultAsync(
                b => b.EntityType == entityType &&
                     b.ViewState == viewState &&
                     b.IsDefault,
                ct);

        if (binding?.Template != null)
        {
            binding.Template.UsageType = usageType;
            return binding.Template;
        }

        // 如果不存在，生成新模板并创建绑定
        var generated = await _generator.GenerateAsync(entityDefinition, viewState, ct);
        generated.UsageType = usageType;
        generated.EntityType ??= entityType;

        _db.FormTemplates.Add(generated);
        await _db.SaveChangesAsync(ct);

        // 创建默认绑定
        var newBinding = new TemplateStateBinding
        {
            EntityType = entityType,
            ViewState = viewState,
            TemplateId = generated.Id,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        };
        _db.TemplateStateBindings.Add(newBinding);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Generated default template {TemplateId} for {EntityType} ({ViewState}) via GetDefaultTemplateAsync requested by {RequestedBy}",
            generated.Id,
            entityType,
            MapViewStateToUsage(viewState),
            requestedBy ?? "system");

        return generated;
    }

    private static string MapUsageToViewState(FormTemplateUsageType usage) => usage switch
    {
        FormTemplateUsageType.List => "List",
        FormTemplateUsageType.Edit => "DetailEdit",
        FormTemplateUsageType.Combined => "Create",
        _ => "DetailView"
    };

    private static FormTemplateUsageType MapViewStateToUsage(string viewState) => viewState switch
    {
        "List" => FormTemplateUsageType.List,
        "DetailEdit" => FormTemplateUsageType.Edit,
        "Create" => FormTemplateUsageType.Combined,
        _ => FormTemplateUsageType.Detail
    };
}
