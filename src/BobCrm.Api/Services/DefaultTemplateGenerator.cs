using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

/// <summary>
/// 根据实体字段元数据生成系统默认模板。
/// </summary>
public class DefaultTemplateGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

    private readonly AppDbContext _db;
    private readonly ILogger<DefaultTemplateGenerator> _logger;

    public DefaultTemplateGenerator(AppDbContext db, ILogger<DefaultTemplateGenerator> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<DefaultTemplateGenerationResult> EnsureTemplatesAsync(
        EntityDefinition entity,
        CancellationToken ct = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        var entityType = entity.EntityRoute;
        var fields = entity.Fields
            .Where(f => !f.IsDeleted)
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.PropertyName)
            .ToList();

        if (fields.Count == 0)
        {
            _logger.LogWarning("[TemplateGenerator] Entity {Entity} has no fields, skipping template generation", entityType);
            return new DefaultTemplateGenerationResult();
        }

        var result = new DefaultTemplateGenerationResult();
        var usages = new[]
        {
            FormTemplateUsageType.Detail,
            FormTemplateUsageType.Edit,
            FormTemplateUsageType.List
        };

        foreach (var usage in usages)
        {
            var template = await _db.FormTemplates
                .FirstOrDefaultAsync(
                    t => t.EntityType == entityType &&
                         t.IsSystemDefault &&
                         t.UsageType == usage,
                    ct);

            var layoutJson = BuildLayoutJson(fields, usage);

            if (template == null)
            {
                template = new FormTemplate
                {
                    Name = $"{entity.EntityName} {usage} Template",
                    EntityType = entityType,
                    UserId = "__system__",
                    IsSystemDefault = true,
                    LayoutJson = layoutJson,
                    UsageType = usage,
                    Description = $"Auto generated {usage} template for {entity.EntityName}",
                    Tags = new List<string> { "auto-generated" },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.FormTemplates.Add(template);
                result.Created.Add(template);
            }
            else
            {
                if (template.LayoutJson != layoutJson)
                {
                    template.LayoutJson = layoutJson;
                    template.UpdatedAt = DateTime.UtcNow;
                    result.Updated.Add(template);
                }
            }

            result.Templates[usage] = template;
        }

        if (result.Created.Count > 0 || result.Updated.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
        }

        return result;
    }

    private static string BuildLayoutJson(IReadOnlyList<FieldMetadata> fields, FormTemplateUsageType usage)
    {
        var layout = new Dictionary<string, object?>();
        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            var widgetType = ResolveWidgetType(field, usage);
            var label = ResolveLabel(field);

            layout[$"item_{i:000}"] = new Dictionary<string, object?>
            {
                ["type"] = widgetType,
                ["label"] = label,
                ["dataField"] = field.PropertyName,
                ["order"] = i,
                ["Width"] = usage == FormTemplateUsageType.List ? 25 : 48,
                ["Height"] = 32,
                ["visible"] = true,
                ["newLine"] = i % 2 == 0
            };
        }

        return JsonSerializer.Serialize(layout, JsonOptions);
    }

    private static string ResolveWidgetType(FieldMetadata field, FormTemplateUsageType usage)
    {
        if (usage == FormTemplateUsageType.List)
        {
            return "label";
        }

        return field.DataType switch
        {
            FieldDataType.Boolean => "checkbox",
            FieldDataType.Int32 => "number",
            FieldDataType.Int64 => "number",
            FieldDataType.Decimal => "number",
            FieldDataType.DateTime => "calendar",
            FieldDataType.Date => "calendar",
            _ => "textbox"
        };
    }

    private static string ResolveLabel(FieldMetadata field)
    {
        if (field.DisplayName == null || field.DisplayName.Count == 0)
        {
            return field.PropertyName;
        }

        return field.DisplayName.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))
            ?? field.PropertyName;
    }
}

public class DefaultTemplateGenerationResult
{
    public Dictionary<FormTemplateUsageType, FormTemplate> Templates { get; } = new();
    public List<FormTemplate> Created { get; } = new();
    public List<FormTemplate> Updated { get; } = new();
}
