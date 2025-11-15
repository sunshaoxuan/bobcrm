using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Services;

public interface IDefaultTemplateGenerator
{
    DefaultTemplateResult Generate(EntityDefinition entityDefinition);
}

public sealed record DefaultTemplateResult(FormTemplate Template);

public class DefaultTemplateGenerator : IDefaultTemplateGenerator
{
    private const string SystemUserId = "__system__";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public DefaultTemplateResult Generate(EntityDefinition entityDefinition)
    {
        ArgumentNullException.ThrowIfNull(entityDefinition);

        var layoutJson = BuildLayoutJson(entityDefinition);
        var templateName = $"{ResolveEntityDisplayName(entityDefinition)} 默认模板";

        var template = new FormTemplate
        {
            Name = templateName,
            EntityType = entityDefinition.FullName,
            UserId = SystemUserId,
            IsUserDefault = false,
            IsSystemDefault = true,
            LayoutJson = layoutJson,
            UsageType = FormTemplateUsageType.Detail,
            Tags = new List<string> { "system", "auto-generated" },
            Description = "Automatically generated default template.",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsInUse = true
        };

        return new DefaultTemplateResult(template);
    }

    private static string BuildLayoutJson(EntityDefinition entityDefinition)
    {
        var fields = entityDefinition.Fields
            .Where(f => !f.IsDeleted)
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var items = new Dictionary<string, object?>();
        var order = 0;

        foreach (var field in fields)
        {
            items[field.PropertyName] = MapFieldToNode(field, order++);
        }

        var layout = new Dictionary<string, object?>
        {
            ["version"] = 1,
            ["mode"] = "flow",
            ["items"] = items
        };

        return JsonSerializer.Serialize(layout, JsonOptions);
    }

    private static Dictionary<string, object?> MapFieldToNode(FieldMetadata field, int order)
    {
        var widgetType = DetermineWidgetType(field);
        var sizing = DetermineSizing(widgetType);

        var node = new Dictionary<string, object?>
        {
            ["id"] = $"fld_{Sanitize(field.PropertyName)}",
            ["key"] = field.PropertyName,
            ["dataField"] = field.PropertyName,
            ["label"] = ResolveFieldLabel(field),
            ["type"] = widgetType,
            ["order"] = order,
            ["w"] = sizing.LegacyColumns,
            ["width"] = sizing.WidthPercent,
            ["widthUnit"] = "%",
            ["height"] = sizing.Height,
            ["heightUnit"] = "px",
            ["newLine"] = sizing.NewLine,
            ["visible"] = true,
            ["required"] = field.IsRequired
        };

        if (widgetType == "calendar")
        {
            var isDateOnly = string.Equals(field.DataType, FieldDataType.Date, StringComparison.OrdinalIgnoreCase);
            node["format"] = isDateOnly ? "yyyy-MM-dd" : "yyyy-MM-dd HH:mm";
            node["showTime"] = !isDateOnly;
        }
        else if (widgetType == "textarea")
        {
            node["minRows"] = 3;
        }

        if (widgetType == "select")
        {
            node["dataSource"] = new Dictionary<string, object?>
            {
                ["type"] = "entity",
                ["entityId"] = field.ReferencedEntityId
            };
        }

        return node;
    }

    private static string ResolveEntityDisplayName(EntityDefinition entityDefinition)
    {
        if (entityDefinition.DisplayName != null)
        {
            foreach (var pair in entityDefinition.DisplayName)
            {
                if (!string.IsNullOrWhiteSpace(pair.Value))
                {
                    return pair.Value!;
                }
            }
        }

        return entityDefinition.EntityName;
    }

    private static string ResolveFieldLabel(FieldMetadata field)
    {
        if (field.DisplayName != null)
        {
            foreach (var pair in field.DisplayName)
            {
                if (!string.IsNullOrWhiteSpace(pair.Value))
                {
                    return pair.Value!;
                }
            }
        }

        return field.PropertyName;
    }

    private static string DetermineWidgetType(FieldMetadata field)
    {
        if (field.IsEntityRef || string.Equals(field.DataType, FieldDataType.EntityRef, StringComparison.OrdinalIgnoreCase))
        {
            return "select";
        }

        if (string.Equals(field.DataType, FieldDataType.Boolean, StringComparison.OrdinalIgnoreCase))
        {
            return "checkbox";
        }

        if (string.Equals(field.DataType, FieldDataType.DateTime, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(field.DataType, FieldDataType.Date, StringComparison.OrdinalIgnoreCase))
        {
            return "calendar";
        }

        if (string.Equals(field.DataType, FieldDataType.Decimal, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(field.DataType, FieldDataType.Int32, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(field.DataType, FieldDataType.Int64, StringComparison.OrdinalIgnoreCase))
        {
            return "number";
        }

        if (string.Equals(field.DataType, FieldDataType.Text, StringComparison.OrdinalIgnoreCase) ||
            (field.Length.HasValue && field.Length.Value > 200))
        {
            return "textarea";
        }

        return "textbox";
    }

    private static LayoutSizing DetermineSizing(string widgetType)
    {
        return widgetType switch
        {
            "textarea" => new LayoutSizing(12, 100, 120, true),
            "checkbox" => new LayoutSizing(3, 25, 32, false),
            _ => new LayoutSizing(6, 50, 32, false)
        };
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "field";
        }

        var builder = new StringBuilder();
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
            }
            else
            {
                builder.Append('_');
            }
        }

        return builder.ToString();
    }

    private readonly record struct LayoutSizing(int LegacyColumns, int WidthPercent, int Height, bool NewLine);
}
