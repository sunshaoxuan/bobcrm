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
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    // Design tokens for layout generation
    private const string BACKGROUND_COLOR_SECTION = "var(--bg-secondary)"; // Replaces #fafafa
    private const string BACKGROUND_COLOR_CARD = "var(--bg-primary)";       // Replaces #ffffff
    private const int WIDTH_BUTTON_PCT = 10;
    private const int WIDTH_SEARCH_PCT = 30;
    private const int WIDTH_FIELD_PCT = 48;
    private const int WIDTH_COLUMN_PX = 150;

    private readonly AppDbContext? _db;
    private readonly ILogger<DefaultTemplateGenerator>? _logger;

    private sealed record ListColumn(
        string? field,
        string label,
        int width,
        bool sortable,
        Guid? enumDefinitionId,
        bool isMultiSelect);

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
            Name = BuildTemplateNameKey(entity, usageType),
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
        bool force = false,
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
            var nameKey = BuildTemplateNameKey(entity, usage);
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
                    Name = nameKey,
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
                if (!string.Equals(template.Name, nameKey, StringComparison.Ordinal))
                {
                    template.Name = nameKey;
                    result.Updated.Add(template);
                }
                
                // Check force flag first to prevent unnecessary updates
                if (force)
                {
                    // Force update requested (manual regeneration)
                    if (template.LayoutJson != layoutJson)
                    {
                        template.LayoutJson = layoutJson;
                    }
                    template.UpdatedAt = DateTime.UtcNow;
                    if (!result.Updated.Contains(template))
                    {
                        result.Updated.Add(template);
                    }
                }
                else if (template.LayoutJson != layoutJson)
                {
                    // Content actually changed, update layout and timestamp
                    template.LayoutJson = layoutJson;
                    template.UpdatedAt = DateTime.UtcNow;
                    if (!result.Updated.Contains(template))
                    {
                        result.Updated.Add(template);
                    }
                }
                // If force=false and content same, preserve existing timestamps
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

    private static string BuildTemplateNameKey(EntityDefinition entity, FormTemplateUsageType usage)
    {
        var entityToken = (entity.EntityRoute ?? entity.EntityName ?? "ENTITY")
            .Replace("-", "_")
            .ToUpperInvariant();
        var usageToken = usage.ToString().ToUpperInvariant();
        return $"TEMPLATE_NAME_{usageToken}_{entityToken}";
    }

    private static string BuildLayoutJson(EntityDefinition entity, IReadOnlyList<FieldMetadata> fields, FormTemplateUsageType usage)
    {
        var widgets = new List<Dictionary<string, object?>>();

        if (usage == FormTemplateUsageType.List)
        {
            var columns = fields.Take(8).Select(f => new ListColumn(
                field: f.PropertyName?.ToLowerInvariant(),
                label: ResolveLabel(f),
                width: WIDTH_COLUMN_PX,
                sortable: true,
                enumDefinitionId: f.EnumDefinitionId,
                isMultiSelect: f.IsMultiSelect)).ToList();

            if (columns.Count == 0)
            {
                columns.Add(new ListColumn(
                    field: "name",
                    label: "LBL_NAME",
                    width: WIDTH_COLUMN_PX,
                    sortable: true,
                    enumDefinitionId: null,
                    isMultiSelect: false));
            }

            var bulkActions = new[]
            {
                new { action = "delete", label = "BTN_DELETE", icon = "delete" }
            };

            // 定义行操作（View, Edit, Delete）
            var rowActions = new[]
            {
                new { action = "view", label = "BTN_VIEW", icon = "eye" },
                new { action = "edit", label = "BTN_EDIT", icon = "edit" },
                new { action = "delete", label = "BTN_DELETE", icon = "delete" }
            };

            var dataGrid = new Dictionary<string, object?>
            {
                ["id"] = Guid.NewGuid().ToString(),
                ["type"] = "datagrid",
                ["label"] = "",
                ["entityType"] = entity.EntityRoute,
                ["apiEndpoint"] = entity.ApiEndpoint ?? $"/api/{entity.EntityRoute}s",
                ["columnsJson"] = JsonSerializer.Serialize(columns, JsonOptions),
                ["defaultSortField"] = columns.First().field,
                ["defaultSortDirection"] = "asc",
                ["bulkActionsJson"] = JsonSerializer.Serialize(bulkActions, JsonOptions),
                ["rowActionsJson"] = JsonSerializer.Serialize(rowActions, JsonOptions),
                ["showPagination"] = true,
                ["pageSize"] = 20,
                ["allowMultiSelect"] = true,
                ["showSearch"] = true,
                ["searchPlaceholderKey"] = "LBL_SEARCH",
                ["showRefreshButton"] = true,
                ["showBordered"] = true,
                ["size"] = "middle",
                ["emptyTextKey"] = "MSG_NO_DATA"
            };
            widgets.Add(dataGrid);
        }
        else
        {
            // For Detail/Edit, we create a Card to group field widgets
            var fieldWidgets = new List<Dictionary<string, object?>>();

            // Add action buttons at the top for Edit mode
            if (usage == FormTemplateUsageType.Edit)
            {
                var buttonSection = new Dictionary<string, object?>
                {
                    ["id"] = Guid.NewGuid().ToString(),
                    ["type"] = "section",
                    ["label"] = "",
                    ["showTitle"] = false,
                    ["children"] = new List<Dictionary<string, object?>>
                    {
                        new()
                        {
                            ["id"] = Guid.NewGuid().ToString(),
                            ["type"] = "button",
                            ["label"] = "BTN_SAVE",
                            ["action"] = "save",
                            ["buttonType"] = "primary",
                            ["width"] = WIDTH_BUTTON_PCT,
                            ["widthUnit"] = "%"
                        },
                        new()
                        {
                            ["id"] = Guid.NewGuid().ToString(),
                            ["type"] = "button",
                            ["label"] = "BTN_BACK",
                            ["action"] = "cancel",
                            ["buttonType"] = "default",
                            ["width"] = WIDTH_BUTTON_PCT,
                            ["widthUnit"] = "%"
                        },
                        new()
                        {
                            ["id"] = Guid.NewGuid().ToString(),
                            ["type"] = "button",
                            ["label"] = "BTN_CANCEL",
                            ["action"] = "cancel",
                            ["buttonType"] = "default",
                            ["width"] = WIDTH_BUTTON_PCT,
                            ["widthUnit"] = "%"
                        }
                    },
                    ["containerLayout"] = new Dictionary<string, object?>
                    {
                        ["flexDirection"] = "row",
                        ["justifyContent"] = "flex-start",
                        ["gap"] = 12,
                        ["padding"] = 12
                    }
                };
                widgets.Add(buttonSection);
            }

            // Create field widgets
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
                    ["width"] = WIDTH_FIELD_PCT,
                    ["widthUnit"] = "%",
                    ["visible"] = true
                };

                // Add enum-specific metadata
                if (field.DataType == FieldDataType.Enum && field.EnumDefinitionId.HasValue)
                {
                    widget["enumDefinitionId"] = field.EnumDefinitionId.Value.ToString();
                    widget["isMultiSelect"] = field.IsMultiSelect;
                }

                fieldWidgets.Add(widget);
            }

            if (fieldWidgets.Count > 0)
            {
                widgets.AddRange(fieldWidgets);
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
