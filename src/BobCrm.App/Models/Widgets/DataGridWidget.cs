using System;
using System.Collections.Generic;
using BobCrm.App.Models.Designer;

namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 数据网格控件 - 通用列表/表格控件,支持数据源绑定、分页、排序、筛选
/// </summary>
public class DataGridWidget : DraggableWidget
{
    public override Type? PreviewComponentType => typeof(BobCrm.App.Components.Designer.WidgetPreviews.DataGridPreview);

    /// <summary>
    /// 数据源ID(关联到 DataSet)
    /// </summary>
    public int? DataSetId { get; set; }

    /// <summary>
    /// 数据源类型(临时属性,用于设计态选择)
    /// </summary>
    public string? DataSourceType { get; set; }

    /// <summary>
    /// 实体类型(当数据源为实体时使用)
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// API端点(当数据源为API时使用)
    /// </summary>
    public string? ApiEndpoint { get; set; }

    /// <summary>
    /// 列定义(JSON格式)
    /// 格式: [{ "field": "code", "label": "COL_CODE", "width": 120, "sortable": true }, ...]
    /// </summary>
    public string? ColumnsJson { get; set; }

    /// <summary>
    /// 是否显示分页
    /// </summary>
    public bool ShowPagination { get; set; } = true;

    /// <summary>
    /// 每页记录数
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 是否允许多选
    /// </summary>
    public bool AllowMultiSelect { get; set; } = false;

    /// <summary>
    /// 是否显示搜索框
    /// </summary>
    public bool ShowSearch { get; set; } = false;

    /// <summary>
    /// 搜索框占位符文本的多语键
    /// </summary>
    public string? SearchPlaceholderKey { get; set; }

    /// <summary>
    /// 是否显示刷新按钮
    /// </summary>
    public bool ShowRefreshButton { get; set; } = true;

    /// <summary>
    /// 批量操作按钮定义(JSON格式)
    /// 格式: [{ "action": "delete", "label": "BTN_DELETE", "icon": "delete" }, ...]
    /// </summary>
    public string? BulkActionsJson { get; set; }

    /// <summary>
    /// 行操作按钮定义(JSON格式)
    /// 格式: [{ "action": "edit", "label": "BTN_EDIT", "icon": "edit" }, ...]
    /// </summary>
    public string? RowActionsJson { get; set; }

    /// <summary>
    /// 默认排序字段
    /// </summary>
    public string? DefaultSortField { get; set; }

    /// <summary>
    /// 默认排序方向(asc/desc)
    /// </summary>
    public string DefaultSortDirection { get; set; } = "asc";

    /// <summary>
    /// 筛选器定义(JSON格式,预留)
    /// 格式: [{ "field": "status", "operator": "eq", "options": [...] }, ...]
    /// </summary>
    public string? FiltersJson { get; set; }

    /// <summary>
    /// 是否显示列边框
    /// </summary>
    public bool ShowBordered { get; set; } = true;

    /// <summary>
    /// 表格大小(small/middle/large)
    /// </summary>
    public string Size { get; set; } = "middle";

    /// <summary>
    /// 空数据时显示的文本多语键
    /// </summary>
    public string? EmptyTextKey { get; set; }

    public override string GetDefaultCodePrefix() => "datagrid";

    public override List<WidgetPropertyMetadata> GetPropertyMetadata()
    {
        var props = base.GetPropertyMetadata();

        props.AddRange(new[]
        {
            new WidgetPropertyMetadata
            {
                PropertyPath = "DataSetId",
                Label = "PROP_DATASET",
                EditorType = PropertyEditorType.DataSetPicker,
                Group = "PROP_GROUP_DATA"
            },
            new WidgetPropertyMetadata
            {
                PropertyPath = "EntityType",
                Label = "PROP_ENTITY_TYPE",
                EditorType = PropertyEditorType.Text,
                Group = "PROP_GROUP_DATA"
            },
            new WidgetPropertyMetadata
            {
                PropertyPath = "ColumnsJson",
                Label = "PROP_COLUMNS",
                EditorType = PropertyEditorType.Json,
                Group = "PROP_GROUP_DATA"
            },
            new WidgetPropertyMetadata
            {
                PropertyPath = "ShowPagination",
                Label = "PROP_SHOW_PAGINATION",
                EditorType = PropertyEditorType.Boolean,
                Group = "PROP_GROUP_DISPLAY"
            },
            new WidgetPropertyMetadata
            {
                PropertyPath = "PageSize",
                Label = "PROP_PAGE_SIZE",
                EditorType = PropertyEditorType.Number,
                Min = 5,
                Max = 100,
                Group = "PROP_GROUP_DISPLAY"
            },
            new WidgetPropertyMetadata
            {
                PropertyPath = "AllowMultiSelect",
                Label = "PROP_ALLOW_MULTISELECT",
                EditorType = PropertyEditorType.Boolean,
                Group = "PROP_GROUP_BEHAVIOR"
            },
            new WidgetPropertyMetadata
            {
                PropertyPath = "ShowSearch",
                Label = "PROP_SHOW_SEARCH",
                EditorType = PropertyEditorType.Boolean,
                Group = "PROP_GROUP_DISPLAY"
            },
            new WidgetPropertyMetadata
            {
                PropertyPath = "DefaultSortField",
                Label = "PROP_DEFAULT_SORT_FIELD",
                EditorType = PropertyEditorType.Text,
                Group = "PROP_GROUP_DATA"
            },
            new WidgetPropertyMetadata
            {
                PropertyPath = "DefaultSortDirection",
                Label = "PROP_DEFAULT_SORT_DIR",
                EditorType = PropertyEditorType.Select,
                Group = "PROP_GROUP_DATA",
                Options = new()
                {
                    new PropertyOption { Value = "asc", Label = "PROP_SORT_ASC" },
                    new PropertyOption { Value = "desc", Label = "PROP_SORT_DESC" }
                }
            },
            new WidgetPropertyMetadata
            {
                PropertyPath = "Size",
                Label = "PROP_TABLE_SIZE",
                EditorType = PropertyEditorType.Select,
                Group = "PROP_GROUP_DISPLAY",
                Options = new()
                {
                    new PropertyOption { Value = "small", Label = "PROP_SIZE_SMALL" },
                    new PropertyOption { Value = "middle", Label = "PROP_SIZE_MIDDLE" },
                    new PropertyOption { Value = "large", Label = "PROP_SIZE_LARGE" }
                }
            }
        });

        return props;
    }

    public override void RenderRuntime(RuntimeRenderContext context)
    {
        var builder = context.Builder;

        // 使用 DataGridRuntime 组件进行渲染
        builder.OpenComponent<BobCrm.App.Components.Shared.DataGridRuntime>(0);
        builder.AddAttribute(1, "Widget", this);
        builder.AddAttribute(2, "ContainerStyle", "");
        builder.CloseComponent();
    }
}
