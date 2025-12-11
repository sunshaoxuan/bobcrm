using System.Collections.Generic;
using BobCrm.App.Models.Designer;
using AntDesign;
using BobCrm.App.Services.Widgets;

namespace BobCrm.App.Models.Widgets;

[WidgetMetadata("orgtree", "LBL_ORGTREE", "Outline.Apartment", WidgetRegistry.WidgetCategory.Data)]
/// <summary>
/// 组织树控件 - 显示和操作组织节点的树形结构
/// </summary>
public class OrganizationTreeWidget : DraggableWidget
{
    /// <summary>
    /// 是否支持多选
    /// </summary>
    public bool AllowMultiSelect { get; set; } = false;

    /// <summary>
    /// 是否显示搜索框
    /// </summary>
    public bool ShowSearch { get; set; } = true;

    /// <summary>
    /// 搜索框占位符的多语键
    /// </summary>
    public string? SearchPlaceholderKey { get; set; }

    /// <summary>
    /// 是否可拖拽排序
    /// </summary>
    public bool AllowDragDrop { get; set; } = false;

    /// <summary>
    /// 是否显示节点操作按钮
    /// </summary>
    public bool ShowNodeActions { get; set; } = true;

    /// <summary>
    /// 节点操作定义(JSON格式)
    /// 格式: [{ "action": "add", "label": "BTN_ADD", "icon": "plus" }, ...]
    /// </summary>
    public string? NodeActionsJson { get; set; }

    /// <summary>
    /// 是否懒加载子节点
    /// </summary>
    public bool LazyLoad { get; set; } = false;

    /// <summary>
    /// 默认展开层级(0表示全部折叠,-1表示全部展开)
    /// </summary>
    public int DefaultExpandLevel { get; set; } = 1;

    /// <summary>
    /// 是否显示根节点
    /// </summary>
    public bool ShowRootNode { get; set; } = true;

    /// <summary>
    /// 根节点名称的多语键
    /// </summary>
    public string? RootNodeLabelKey { get; set; }

    /// <summary>
    /// 节点图标字段(如果为空则使用默认图标)
    /// </summary>
    public string? NodeIconField { get; set; }

    /// <summary>
    /// 是否显示节点路径
    /// </summary>
    public bool ShowNodePath { get; set; } = false;

    /// <summary>
    /// 选中变化时的回调动作(预留,用于行为脚本)
    /// </summary>
    public string? OnSelectChangeAction { get; set; }

    public override string GetDefaultCodePrefix() => "orgtree";

    public override List<WidgetPropertyMetadata> GetPropertyMetadata()
    {
        var props = base.GetPropertyMetadata();

        props.AddRange(new[]
        {
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
                PropertyPath = "AllowDragDrop",
                Label = "PROP_ALLOW_DRAG_DROP",
                EditorType = PropertyEditorType.Boolean,
                Group = "PROP_GROUP_BEHAVIOR"
            },
            new WidgetPropertyMetadata
            {
                PropertyPath = "ShowNodeActions",
                Label = "PROP_SHOW_NODE_ACTIONS",
                EditorType = PropertyEditorType.Boolean,
                Group = "PROP_GROUP_DISPLAY"
            },
            new WidgetPropertyMetadata
            {
                PropertyPath = "LazyLoad",
                Label = "PROP_LAZY_LOAD",
                EditorType = PropertyEditorType.Boolean,
                Group = "PROP_GROUP_BEHAVIOR"
            },
            new WidgetPropertyMetadata
            {
                PropertyPath = "DefaultExpandLevel",
                Label = "PROP_DEFAULT_EXPAND_LEVEL",
                EditorType = PropertyEditorType.Number,
                Min = -1,
                Max = 10,
                Group = "PROP_GROUP_DISPLAY"
            },
            new WidgetPropertyMetadata
            {
                PropertyPath = "ShowRootNode",
                Label = "PROP_SHOW_ROOT_NODE",
                EditorType = PropertyEditorType.Boolean,
                Group = "PROP_GROUP_DISPLAY"
            }
        });

        return props;
    }

    public override void RenderRuntime(RuntimeRenderContext context)
    {
        var builder = context.Builder;

        // 运行态渲染 - 组织树控件
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "org-tree-runtime");
        builder.AddAttribute(2, "style", "padding:16px; background:#fff; border:1px solid #d9d9d9; border-radius:4px;");

        // 标题
        if (!string.IsNullOrWhiteSpace(Label))
        {
            builder.OpenElement(3, "div");
            builder.AddAttribute(4, "style", "font-weight:600; margin-bottom:12px; font-size:16px;");
            builder.AddContent(5, Label);
            builder.CloseElement();
        }

        // 搜索框(如果启用)
        if (ShowSearch)
        {
            builder.OpenElement(6, "input");
            builder.AddAttribute(7, "type", "text");
            builder.AddAttribute(8, "placeholder", SearchPlaceholderKey ?? "Search organizations...");
            builder.AddAttribute(9, "style", "width:100%; padding:8px; margin-bottom:12px; border:1px solid #d9d9d9; border-radius:2px;");
            builder.CloseElement();
        }

        // 树结构容器
        builder.OpenElement(10, "div");
        builder.AddAttribute(11, "class", "tree-container");
        builder.AddAttribute(12, "style", "min-height:200px; max-height:400px; overflow-y:auto; border:1px solid #e0e0e0; border-radius:2px; padding:8px; background:#fafafa;");

        // 示例树节点(实际应从 /api/organization API 加载组织数据)
        builder.OpenElement(13, "div");
        builder.AddAttribute(14, "style", "padding:4px 8px; cursor:pointer; border-radius:2px; hover:background:#e6f7ff;");
        builder.AddContent(15, "📁 Root Organization");
        builder.OpenElement(16, "div");
        builder.AddAttribute(17, "style", "margin-left:20px; margin-top:4px;");
        builder.OpenElement(18, "div");
        builder.AddAttribute(19, "style", "padding:4px 8px; cursor:pointer;");
        builder.AddContent(20, "📁 Department A");
        builder.CloseElement();
        builder.OpenElement(21, "div");
        builder.AddAttribute(22, "style", "padding:4px 8px; cursor:pointer;");
        builder.AddContent(23, "📁 Department B");
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();

        builder.CloseElement(); // tree-container

        // 配置信息提示
        builder.OpenElement(24, "div");
        builder.AddAttribute(25, "style", "margin-top:8px; font-size:11px; color:#999; display:flex; gap:12px;");
        builder.OpenElement(26, "span");
        builder.AddContent(27, $"Multi-select: {AllowMultiSelect}");
        builder.CloseElement();
        builder.OpenElement(28, "span");
        builder.AddContent(29, $"Lazy-load: {LazyLoad}");
        builder.CloseElement();
        builder.OpenElement(30, "span");
        builder.AddContent(31, $"Expand level: {DefaultExpandLevel}");
        builder.CloseElement();
        builder.CloseElement();

        builder.CloseElement(); // container
    }

    public override void RenderDesign(DesignRenderContext context)
    {
        var builder = context.Builder;

        // 设计态预览 - 显示树形结构占位符
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"padding:8px; background:{context.BackgroundResolver(this)}; border:1px dashed #d9d9d9;");

        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "style", $"{context.TextStyleResolver(this)} font-weight:bold; margin-bottom:8px;");
        builder.AddContent(4, context.Localize(Label));
        builder.CloseElement();

        // 树形占位符
        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "style", "padding-left:8px;");

        // 根节点
        RenderTreeNodePlaceholder(builder, "Root Organization", 0);

        // 子节点
        builder.OpenElement(8, "div");
        builder.AddAttribute(9, "style", "padding-left:16px;");
        RenderTreeNodePlaceholder(builder, "Department 1", 1);
        RenderTreeNodePlaceholder(builder, "Department 2", 1);
        builder.CloseElement();

        builder.CloseElement();
        builder.CloseElement();
    }

    private void RenderTreeNodePlaceholder(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, string text, int level)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", "padding:4px 0; font-size:11px; color:#666; display:flex; align-items:center; gap:4px;");

        // 图标
        builder.OpenElement(2, "span");
        builder.AddAttribute(3, "style", "color:#999;");
        builder.AddContent(4, level == 0 ? "📁" : "📄");
        builder.CloseElement();

        // 文本
        builder.OpenElement(5, "span");
        builder.AddContent(6, text);
        builder.CloseElement();

        builder.CloseElement();
    }
}
