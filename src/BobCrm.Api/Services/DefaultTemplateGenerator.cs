using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BobCrm.Api.Services;

public interface IDefaultTemplateGenerator
{
    DefaultTemplateModelResult Generate(
        EntityDefinition entity,
        FormTemplateUsageType usageType = FormTemplateUsageType.Detail);

    Task<DefaultTemplateGenerationResult> EnsureTemplatesAsync(
        EntityDefinition entity,
        CancellationToken ct = default);
}

/// <summary>
/// 根据实体字段元数据生成系统默认模板。
/// </summary>
public class DefaultTemplateGenerator : IDefaultTemplateGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

    private readonly AppDbContext? _db;
    private readonly ILogger<DefaultTemplateGenerator>? _logger;

    public DefaultTemplateGenerator()
    {
    }

    public DefaultTemplateGenerator(AppDbContext db, ILogger<DefaultTemplateGenerator> logger)
    {
        _db = db;
        _logger = logger;
    }

    public DefaultTemplateModelResult Generate(
        EntityDefinition entity,
        FormTemplateUsageType usageType = FormTemplateUsageType.Detail)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var fields = PrepareFields(entity);
        if (fields.Count == 0)
        {
            throw new InvalidOperationException(
                $"Entity '{entity.EntityName}' does not define any fields for template generation.");
        }

        var template = new FormTemplate
        {
            Name = $"{entity.EntityName} {usageType} Template",
            EntityType = entity.EntityRoute,
            UserId = "__system__",
            IsSystemDefault = true,
            IsUserDefault = false,
            IsInUse = true,
            LayoutJson = BuildLayoutJson(fields, usageType),
            UsageType = usageType,
            Description = $"Auto generated {usageType} template for {entity.EntityName}",
            Tags = new List<string> { "auto-generated" },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return new DefaultTemplateModelResult(template);
    }

    public async Task<DefaultTemplateGenerationResult> EnsureTemplatesAsync(
        EntityDefinition entity,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (_db == null)
        {
            throw new InvalidOperationException(
                "EnsureTemplatesAsync requires DefaultTemplateGenerator to be created with AppDbContext.");
        }

        var entityType = entity.EntityRoute;
        var fields = PrepareFields(entity);

        if (fields.Count == 0)
        {
            _logger?.LogWarning(
                "[TemplateGenerator] Entity {Entity} has no fields, skipping template generation",
                entityType);
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

    private static List<FieldMetadata> PrepareFields(EntityDefinition entity)
    {
        return entity.Fields?
                   .Where(f => !f.IsDeleted)
                   .OrderBy(f => f.SortOrder)
                   .ThenBy(f => f.PropertyName)
                   .ToList()
               ?? new List<FieldMetadata>();
    }

    private static string BuildLayoutJson(IReadOnlyList<FieldMetadata> fields, FormTemplateUsageType usage)
    {
        var items = new Dictionary<string, object?>();
        var columns = usage == FormTemplateUsageType.List ? 4 : 2;
        var widthPercent = 100 / Math.Max(1, columns);
        var legacyWidth = Math.Max(1, (int)Math.Round(widthPercent / 100m * 12m));

        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            var widgetType = ResolveWidgetType(field, usage);
            var label = ResolveLabel(field);

            var item = new Dictionary<string, object?>
            {
                ["id"] = $"{field.PropertyName}_{usage.ToString().ToLowerInvariant()}",
                ["type"] = widgetType,
                ["label"] = label,
                ["dataField"] = field.PropertyName,
                ["order"] = i,
                ["w"] = legacyWidth,
                ["Width"] = widthPercent,
                ["WidthUnit"] = "%",
                ["Height"] = 32,
                ["HeightUnit"] = "px",
                ["visible"] = true,
                ["newLine"] = usage == FormTemplateUsageType.List ? false : i % columns == 0
            };

            if (field.IsRequiredExplicitlySet)
            {
                item["required"] = field.IsRequired;
            }

            items[field.PropertyName] = item;
        }

        var layout = new Dictionary<string, object?>
        {
            ["mode"] = usage == FormTemplateUsageType.List ? "table" : "flow",
            ["items"] = items
        };

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
            FieldDataType.EntityRef => "select",
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

public record DefaultTemplateModelResult(FormTemplate Template);
