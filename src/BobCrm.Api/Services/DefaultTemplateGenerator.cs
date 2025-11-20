using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Application.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BobCrm.Api.Services;

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

    public Task<FormTemplate> GenerateAsync(
        EntityDefinition entity,
        FormTemplateUsageType usageType = FormTemplateUsageType.Detail,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        cancellationToken.ThrowIfCancellationRequested();

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
            LayoutJson = BuildLayoutJson(entity, fields, usageType),
            UsageType = usageType,
            Description = $"Auto generated {usageType} template for {entity.EntityName}",
            Tags = new List<string> { "auto-generated" },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return Task.FromResult(template);
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

            var layoutJson = BuildLayoutJson(entity, fields, usage);

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

    private static string BuildLayoutJson(EntityDefinition entity, IReadOnlyList<FieldMetadata> fields, FormTemplateUsageType usage)
    {
        var widgets = new List<Dictionary<string, object?>>();

        if (usage == FormTemplateUsageType.List)
        {
            // For List, we create a single DataGridWidget
            var columns = fields.Select(f => new
            {
                field = f.PropertyName?.ToLowerInvariant(),
                label = ResolveLabel(f),
                width = 150,
                sortable = true
            }).ToList();

            // 定义行操作（Edit和Delete）
            var rowActions = new[]
            {
                new { action = "edit", label = "Edit", icon = "edit" },
                new { action = "delete", label = "Delete", icon = "delete" }
            };

            var dataGrid = new Dictionary<string, object?>
            {
                ["id"] = Guid.NewGuid().ToString(),
                ["type"] = "datagrid",
                ["label"] = $"{entity.DisplayName?.Values.FirstOrDefault() ?? entity.EntityName} List",
                ["entityType"] = entity.EntityRoute,
                ["apiEndpoint"] = entity.ApiEndpoint ?? $"/api/{entity.EntityRoute}s", // Simple pluralization fallback
                ["columnsJson"] = JsonSerializer.Serialize(columns, JsonOptions),
                ["rowActionsJson"] = JsonSerializer.Serialize(rowActions, JsonOptions),
                ["showPagination"] = true,
                ["pageSize"] = 20,
                ["allowMultiSelect"] = true,
                ["showSearch"] = true,
                ["showRefreshButton"] = true,
                ["showBordered"] = false
            };
            widgets.Add(dataGrid);
        }
        else
        {
            // For Detail/Edit, we create a list of field widgets
            foreach (var field in fields)
            {
                var propertyName = field.PropertyName?.Trim();
                if (string.IsNullOrWhiteSpace(propertyName)) continue;

                var widgetType = ResolveWidgetType(field, usage);
                var label = ResolveLabel(field);

                var widget = new Dictionary<string, object?>
                {
                    ["id"] = Guid.NewGuid().ToString(),
                    ["type"] = widgetType,
                    ["label"] = label,
                    ["dataField"] = propertyName,
                    ["required"] = field.IsRequired,
                    ["w"] = 6, // Half width (12 grid system)
                    ["visible"] = true
                };

                // Add enum-specific metadata
                if (field.DataType == FieldDataType.Enum && field.EnumDefinitionId.HasValue)
                {
                    widget["enumDefinitionId"] = field.EnumDefinitionId.Value.ToString();
                    widget["isMultiSelect"] = field.IsMultiSelect;
                }

                widgets.Add(widget);
            }

            // 根据实体路由追加专用控件
            if (entity.EntityRoute?.ToLowerInvariant() == "users")
            {
                // 为 User 实体添加角色分配控件
                var userRoleWidget = new Dictionary<string, object?>
                {
                    ["id"] = Guid.NewGuid().ToString(),
                    ["type"] = "userrole",
                    ["label"] = "LBL_USER_ROLE_ASSIGNMENT",
                    ["userIdField"] = "Id",
                    ["showSearch"] = true,
                    ["showSelectAll"] = true,
                    ["showCurrentRoles"] = true,
                    ["showOrganizationScope"] = true,
                    ["readOnly"] = false,
                    ["w"] = 12, // Full width
                    ["visible"] = true
                };
                widgets.Add(userRoleWidget);
            }
            else if (entity.EntityRoute?.ToLowerInvariant() == "roles")
            {
                // 为 Role 实体添加权限树控件
                var permTreeWidget = new Dictionary<string, object?>
                {
                    ["id"] = Guid.NewGuid().ToString(),
                    ["type"] = "permtree",
                    ["label"] = "LBL_ROLE_PERMISSIONS",
                    ["roleIdField"] = "Id",
                    ["showSearch"] = true,
                    ["showSelectAll"] = true,
                    ["showExpandAll"] = true,
                    ["showNodeIcons"] = true,
                    ["showTemplateBindings"] = false,
                    ["defaultExpandLevel"] = 1,
                    ["readOnly"] = false,
                    ["cascadeSelect"] = true,
                    ["showStatistics"] = true,
                    ["w"] = 12, // Full width
                    ["visible"] = true
                };
                widgets.Add(permTreeWidget);
            }
        }

        return JsonSerializer.Serialize(widgets, JsonOptions);
    }

    private static string ResolveWidgetType(FieldMetadata field, FormTemplateUsageType usage)
    {
        if (usage == FormTemplateUsageType.List) return "label"; // Not used for DataGrid columns directly

        if (IsLongText(field)) return "textarea";

        return field.DataType switch
        {
            FieldDataType.Boolean => "checkbox",
            FieldDataType.Int32 => "number",
            FieldDataType.Int64 => "number",
            FieldDataType.Decimal => "number",
            FieldDataType.DateTime => "date", // Changed from calendar to date for widget type consistency
            FieldDataType.Date => "date",
            FieldDataType.EntityRef => "select",
            FieldDataType.Enum => "enumselector", // Dynamic enum selector
            _ => "text" // Changed from textbox to text
        };
    }

    private static bool IsLongText(FieldMetadata field)
    {
        return field.Length.HasValue && field.Length > 255;
    }

    private static string ResolveLabel(FieldMetadata field)
    {
        if (field.DisplayName == null || field.DisplayName.Count == 0)
        {
            return field.PropertyName ?? "";
        }

        return field.DisplayName.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))
            ?? field.PropertyName ?? "";
    }
}
