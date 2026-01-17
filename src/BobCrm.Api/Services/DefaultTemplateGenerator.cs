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

    private static string MapUsageToViewState(FormTemplateUsageType usageType) =>
        usageType switch
        {
            FormTemplateUsageType.List => "List",
            FormTemplateUsageType.Edit => "DetailEdit",
            FormTemplateUsageType.Combined => "Create",
            _ => "DetailView"
        };

    private static FormTemplateUsageType MapViewStateToUsage(string viewState) =>
        viewState switch
        {
            "List" => FormTemplateUsageType.List,
            "DetailEdit" => FormTemplateUsageType.Edit,
            "Create" => FormTemplateUsageType.Combined,
            _ => FormTemplateUsageType.Detail
        };

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
        FormTemplateUsageType usageType,
        CancellationToken cancellationToken = default)
    {
        var viewState = MapUsageToViewState(usageType);
        return GenerateAsync(entity, viewState, cancellationToken);
    }

    public Task<FormTemplate> GenerateAsync(
        EntityDefinition entity,
        string viewState = "DetailView",
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
            Name = BuildTemplateNameKey(entity, viewState),
            EntityType = entity.EntityRoute,
            UserId = "__system__",
            IsSystemDefault = true,
            IsUserDefault = false,
            IsInUse = true,
            UsageType = MapViewStateToUsage(viewState),
            LayoutJson = BuildLayoutJson(entity, fields, viewState),
            Description = $"Auto generated {viewState} template for {entity.EntityName}",
            Tags = new List<string> { "auto-generated" },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return Task.FromResult(template);
    }

    public async Task<DefaultTemplateGenerationResult> EnsureTemplatesAsync(
        EntityDefinition entity,
        bool force = false,
        bool saveChanges = true,
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
        var viewStates = new[] { "List", "DetailView", "DetailEdit" };

        foreach (var viewState in viewStates)
        {
            _logger?.LogInformation("[TemplateGenerator] Ensuring template for {ViewState}", viewState);
            var fieldsForUsage = force
                ? (entity.Fields?.ToList() ?? new List<FieldMetadata>())
                : PrepareFields(entity);
            if (fieldsForUsage.Count == 0)
            {
                continue;
            }
                var nameKey = BuildTemplateNameKey(entity, viewState);

                // 查询通过 TemplateStateBinding 关联的模板
                var binding = await _db!.TemplateStateBindings
                    .Include(b => b.Template)
                .FirstOrDefaultAsync(
                    b => b.EntityType == entityType &&
                         b.ViewState == viewState &&
                         b.IsDefault,
                    ct);

            FormTemplate? template = binding?.Template;

            var layoutJson = BuildLayoutJson(entity, fieldsForUsage, viewState);
            if (_db != null &&
                string.Equals(entityType, "customer", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(viewState, "DetailView", StringComparison.OrdinalIgnoreCase))
            {
                layoutJson = await TryApplyCustomer360Async(layoutJson, ct);
            }

                if (template == null)
                {
                    // 创建新模板
                    template = new FormTemplate
                    {
                        Name = nameKey,
                        EntityType = entityType,
                        UserId = "__system__",
                        IsSystemDefault = true,
                        UsageType = MapViewStateToUsage(viewState),
                        LayoutJson = layoutJson,
                        Description = $"Auto generated {viewState} template for {entity.EntityName}",
                        Tags = new List<string> { "auto-generated" },
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _db!.FormTemplates.Add(template);
                    if (saveChanges)
                    {
                        await _db!.SaveChangesAsync(ct); // 保存以获取 TemplateId
                    }

                    // 创建默认绑定
                    var newBinding = new TemplateStateBinding
                    {
                        EntityType = entityType,
                        ViewState = viewState,
                        Template = template, // 使用引用而不是 ID，以便在延迟保存时自动处理
                        IsDefault = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db!.TemplateStateBindings.Add(newBinding);

                result.Created.Add(template);
            }
            else
            {
                if (!string.Equals(template.Name, nameKey, StringComparison.Ordinal))
                {
                    template.Name = nameKey;
                    result.Updated.Add(template);
                }

                template.UsageType = MapViewStateToUsage(viewState);

                var expectedCount = viewState == "List"
                    ? Math.Min(fieldsForUsage.Count, 8) // 列表模板只取前 8 个字段生成列
                    : fieldsForUsage.Count;

                var existingFieldCount = CountDataFields(template.LayoutJson ?? string.Empty, viewState);
                var shouldRegenerate = force || existingFieldCount != expectedCount;

                // Manual regeneration or detected schema drift -> refresh layout & timestamp.
                // Startup ensure with no schema drift -> preserve existing layout/timestamp.
                if (shouldRegenerate)
                {
                    template.LayoutJson = EnsureAllFieldsPresent(layoutJson, fieldsForUsage, viewState);
                    template.UpdatedAt = DateTime.UtcNow;
                    if (!result.Updated.Contains(template))
                    {
                        result.Updated.Add(template);
                    }
                }
            }

            result.Templates[viewState] = template;
        }

        if ((result.Created.Count > 0 || result.Updated.Count > 0) && saveChanges)
        {
            _logger?.LogInformation("[TemplateGenerator] Saving {Created} new and {Updated} updated templates", result.Created.Count, result.Updated.Count);
            await _db!.SaveChangesAsync(ct);
        }

        // 保持旧的 TemplateBindings 表与当前模板同步，便于兼容现有流程
        foreach (var kvp in result.Templates)
        {
            var usage = MapViewStateToUsage(kvp.Key);
            var binding = await _db!.TemplateBindings
                .FirstOrDefaultAsync(b => b.EntityType == entityType && b.UsageType == usage && b.IsSystem, ct);

            if (binding == null)
            {
                binding = new TemplateBinding
                {
                    EntityType = entityType,
                    UsageType = usage,
                    IsSystem = true,
                    TemplateId = kvp.Value.Id,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = "system"
                };
                _db!.TemplateBindings.Add(binding);
            }
        }
        if (saveChanges)
        {
            _logger?.LogInformation("[TemplateGenerator] Saving legacy template bindings");
            await _db!.SaveChangesAsync(ct);
        }

        return result;
    }

    private static List<FieldMetadata> PrepareFields(EntityDefinition entity)
    {
        var fields = entity.Fields?
                       .Where(f => !f.IsDeleted)
                       .OrderBy(f => f.SortOrder)
                       .ThenBy(f => f.PropertyName)
                       .ToList()
                   ?? new List<FieldMetadata>();

        // Fallback: ensure基本字段存在，避免生成空模板
        if (fields.Count == 0)
        {
            fields = new List<FieldMetadata>
            {
                new FieldMetadata
                {
                    PropertyName = "code",
                    DataType = FieldDataType.String.ToString(),
                    DisplayName = new Dictionary<string, string?>
                    {
                        ["zh"] = "编码",
                        ["ja"] = "コード",
                        ["en"] = "Code"
                    },
                    IsRequired = true
                },
                new FieldMetadata
                {
                    PropertyName = "name",
                    DataType = FieldDataType.String.ToString(),
                    DisplayName = new Dictionary<string, string?>
                    {
                        ["zh"] = "名称",
                        ["ja"] = "名称",
                        ["en"] = "Name"
                    },
                    IsRequired = true
                }
            };
        }

        return fields;
    }

    private static string BuildTemplateNameKey(EntityDefinition entity, string viewState)
    {
        var entityToken = (entity.EntityRoute ?? entity.EntityName ?? "ENTITY")
            .Replace("-", "_")
            .ToUpperInvariant();
        var stateToken = viewState.ToUpperInvariant();
        return $"TEMPLATE_NAME_{stateToken}_{entityToken}";
    }

    private static string BuildLayoutJson(EntityDefinition entity, IReadOnlyList<FieldMetadata> fields, string viewState)
    {
        var widgets = new List<Dictionary<string, object?>>();

        if (viewState == "List")
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

            // Add action buttons at the top for Edit mode (DetailEdit or Create)
            if (viewState == "DetailEdit" || viewState == "Create")
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

                var widgetType = ResolveWidgetType(field, viewState);
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

    private async Task<string> TryApplyCustomer360Async(string layoutJson, CancellationToken ct)
    {
        if (_db == null)
        {
            return layoutJson;
        }

        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "contact",
            "contacts",
            "opportunity",
            "opportunities"
        };

        var related = await _db.EntityDefinitions
            .AsNoTracking()
            .Include(e => e.Fields)
            .Where(e => candidates.Contains(e.EntityRoute))
            .OrderBy(e => e.Order)
            .ToListAsync(ct);

        if (related.Count == 0)
        {
            return layoutJson;
        }

        List<Dictionary<string, object?>> widgets;
        try
        {
            widgets = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(layoutJson, JsonOptions) ?? new List<Dictionary<string, object?>>();
        }
        catch
        {
            return layoutJson;
        }

        if (widgets.Any(w => w.TryGetValue("type", out var t) && string.Equals(t?.ToString(), "tabbox", StringComparison.OrdinalIgnoreCase)))
        {
            return layoutJson;
        }

        var tabs = new List<Dictionary<string, object?>>();
        var firstTabId = string.Empty;

        foreach (var entity in related)
        {
            var tabId = Guid.NewGuid().ToString("N");
            if (string.IsNullOrWhiteSpace(firstTabId))
            {
                firstTabId = tabId;
            }

            var fields = PrepareFields(entity);
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
                    field: "id",
                    label: "LBL_ID",
                    width: WIDTH_COLUMN_PX,
                    sortable: true,
                    enumDefinitionId: null,
                    isMultiSelect: false));
            }

            var tabLabel = entity.EntityName;
            var dataGrid = new Dictionary<string, object?>
            {
                ["id"] = Guid.NewGuid().ToString(),
                ["type"] = "datagrid",
                ["label"] = "",
                ["entityType"] = entity.EntityRoute,
                ["apiEndpoint"] = string.IsNullOrWhiteSpace(entity.ApiEndpoint) ? $"/api/{entity.EntityRoute}s" : entity.ApiEndpoint,
                ["columnsJson"] = JsonSerializer.Serialize(columns, JsonOptions),
                ["defaultSortField"] = columns.First().field,
                ["defaultSortDirection"] = "asc",
                ["showPagination"] = true,
                ["pageSize"] = 20,
                ["allowMultiSelect"] = false,
                ["showSearch"] = false,
                ["showRefreshButton"] = true,
                ["showBordered"] = true,
                ["size"] = "middle",
                ["emptyTextKey"] = "MSG_NO_DATA",
                ["filterByContext"] = true,
                ["contextKey"] = "Id",
                ["targetField"] = "CustomerId"
            };

            tabs.Add(new Dictionary<string, object?>
            {
                ["id"] = Guid.NewGuid().ToString(),
                ["type"] = "tab",
                ["label"] = tabLabel,
                ["tabId"] = tabId,
                ["isDefault"] = tabId == firstTabId,
                ["children"] = new List<Dictionary<string, object?>> { dataGrid }
            });
        }

        if (tabs.Count == 0)
        {
            return layoutJson;
        }

        widgets.Add(new Dictionary<string, object?>
        {
            ["id"] = Guid.NewGuid().ToString(),
            ["type"] = "tabbox",
            ["label"] = "Related",
            ["activeTabId"] = firstTabId,
            ["children"] = tabs
        });

        return JsonSerializer.Serialize(widgets, JsonOptions);
    }

    private static int CountDataFields(string layoutJson, string viewState)
    {
        if (string.IsNullOrWhiteSpace(layoutJson)) return 0;

        try
        {
            using var doc = JsonDocument.Parse(layoutJson);
            if (viewState == "List")
            {
                // List 模板：解析 DataGrid 的 columnsJson，计算列数量，避免每次启动都被误判为需要重建
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        if (el.TryGetProperty("columnsJson", out var colsJsonElement) &&
                            colsJsonElement.ValueKind == JsonValueKind.String)
                        {
                            var colsJson = colsJsonElement.GetString();
                            if (!string.IsNullOrWhiteSpace(colsJson))
                            {
                                try
                                {
                                    using var colsDoc = JsonDocument.Parse(colsJson);
                                    if (colsDoc.RootElement.ValueKind == JsonValueKind.Array)
                                    {
                                        return colsDoc.RootElement.GetArrayLength();
                                    }
                                }
                                catch
                                {
                                    // ignore parse errors and fallback below
                                }
                            }
                        }
                    }
                }
            }

            var count = 0;
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (el.TryGetProperty("dataField", out _))
                    {
                        count++;
                    }
                }
            }
            return count;
        }
        catch
        {
            return 0;
        }
    }

    private static string EnsureAllFieldsPresent(string layoutJson, IReadOnlyList<FieldMetadata> fields, string viewState)
    {
        // 针对 Detail/Edit：确保新增字段也出现在布局中；List 的列保持生成逻辑，不在此补列。
        if (viewState == "List")
        {
            return layoutJson;
        }

        List<Dictionary<string, object?>> widgets;
        try
        {
            widgets = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(layoutJson, JsonOptions)
                      ?? new List<Dictionary<string, object?>>();
        }
        catch
        {
            widgets = new List<Dictionary<string, object?>>();
        }

        var existing = new HashSet<string>(
            widgets.Select(w => w.TryGetValue("dataField", out var v) ? v?.ToString() ?? string.Empty : string.Empty),
            StringComparer.OrdinalIgnoreCase);

        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field.PropertyName)) continue;
            if (existing.Contains(field.PropertyName)) continue;

            var widgetType = ResolveWidgetType(field, viewState);
            widgets.Add(new Dictionary<string, object?>
            {
                ["id"] = Guid.NewGuid().ToString(),
                ["type"] = widgetType,
                ["label"] = ResolveLabel(field),
                ["dataField"] = field.PropertyName,
                ["required"] = field.IsRequired,
                ["width"] = WIDTH_FIELD_PCT,
                ["widthUnit"] = "%",
                ["visible"] = true
            });
        }

        return JsonSerializer.Serialize(widgets, JsonOptions);
    }

    private static string ResolveWidgetType(FieldMetadata field, string viewState)
    {
        if (viewState == "List") return "label"; // Not used for DataGrid columns directly

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
