using System.Collections.Generic;
using BobCrm.App.Models.Designer;

namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 角色权限树控件 - 显示和编辑角色的功能权限树
/// </summary>
public class RolePermissionTreeWidget : DraggableWidget
{
    /// <summary>
    /// 角色ID字段(用于绑定数据)
    /// </summary>
    public string? RoleIdField { get; set; }

    /// <summary>
    /// 是否显示搜索框
    /// </summary>
    public bool ShowSearch { get; set; } = true;

    /// <summary>
    /// 搜索框占位符的多语键
    /// </summary>
    public string? SearchPlaceholderKey { get; set; }

    /// <summary>
    /// 是否显示全选/全不选按钮
    /// </summary>
    public bool ShowSelectAll { get; set; } = true;

    /// <summary>
    /// 是否显示展开/折叠全部按钮
    /// </summary>
    public bool ShowExpandAll { get; set; } = true;

    /// <summary>
    /// 是否显示节点图标
    /// </summary>
    public bool ShowNodeIcons { get; set; } = true;

    /// <summary>
    /// 是否显示模板绑定信息
    /// </summary>
    public bool ShowTemplateBindings { get; set; } = false;

    /// <summary>
    /// 默认展开层级(0表示全部折叠,-1表示全部展开)
    /// </summary>
    public int DefaultExpandLevel { get; set; } = 1;

    /// <summary>
    /// 是否只读模式(仅查看,不可编辑)
    /// </summary>
    public bool ReadOnly { get; set; } = false;

    /// <summary>
    /// 是否支持父子节点级联选择
    /// </summary>
    public bool CascadeSelect { get; set; } = true;

    /// <summary>
    /// 节点选择变化时的回调动作(预留,用于行为脚本)
    /// </summary>
    public string? OnPermissionChangeAction { get; set; }

    /// <summary>
    /// 是否显示权限统计信息
    /// </summary>
    public bool ShowStatistics { get; set; } = false;

    public override string GetDefaultCodePrefix() => "permtree";

    public override List<WidgetPropertyMetadata> GetPropertyMetadata()
    {
        var props = base.GetPropertyMetadata();

        props.AddRange(new[]
        {
            new WidgetPropertyMetadata
            {
                PropertyPath = "RoleIdField",
                Label = "PROP_ROLE_ID_FIELD",
                EditorType = PropertyEditorType.Text,
                Group = "PROP_GROUP_DATA"
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
                PropertyPath = "ShowSelectAll",
                Label = "PROP_SHOW_SELECT_ALL",
                EditorType = PropertyEditorType.Boolean,
                Group = "PROP_GROUP_DISPLAY"
            },
            new WidgetPropertyMetadata
            {
                PropertyPath = "ShowExpandAll",
                Label = "PROP_SHOW_EXPAND_ALL",
                EditorType = PropertyEditorType.Boolean,
                Group = "PROP_GROUP_DISPLAY"
            },
            new WidgetPropertyMetadata
            {
                PropertyPath = "ShowNodeIcons",
                Label = "PROP_SHOW_NODE_ICONS",
                EditorType = PropertyEditorType.Boolean,
                Group = "PROP_GROUP_DISPLAY"
            },
            new WidgetPropertyMetadata
            {
                PropertyPath = "ShowTemplateBindings",
                Label = "PROP_SHOW_TEMPLATE_BINDINGS",
                EditorType = PropertyEditorType.Boolean,
                Group = "PROP_GROUP_DISPLAY"
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
                PropertyPath = "ReadOnly",
                Label = "PROP_READ_ONLY",
                EditorType = PropertyEditorType.Boolean,
                Group = "PROP_GROUP_BEHAVIOR"
            },
            new WidgetPropertyMetadata
            {
                PropertyPath = "CascadeSelect",
                Label = "PROP_CASCADE_SELECT",
                EditorType = PropertyEditorType.Boolean,
                Group = "PROP_GROUP_BEHAVIOR"
            }
        });

        return props;
    }

    public override void RenderRuntime(RuntimeRenderContext context)
    {
        var builder = context.Builder;

        // 运行态渲染 - 角色权限树控件
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "perm-tree-runtime");
        builder.AddAttribute(2, "style", "padding:16px; background:#fff; border:1px solid #d9d9d9; border-radius:4px;");

        // 标题
        if (!string.IsNullOrWhiteSpace(Label))
        {
            builder.OpenElement(3, "div");
            builder.AddAttribute(4, "style", "font-weight:600; margin-bottom:12px; font-size:16px;");
            builder.AddContent(5, Label);
            builder.CloseElement();
        }

        // 工具栏
        if (ShowSelectAll || ShowExpandAll)
        {
            builder.OpenElement(6, "div");
            builder.AddAttribute(7, "style", "margin-bottom:12px; display:flex; gap:8px;");

            if (ShowSelectAll && !ReadOnly)
            {
                builder.OpenElement(8, "button");
                builder.AddAttribute(9, "style", "padding:4px 12px; border:1px solid #d9d9d9; border-radius:2px; cursor:pointer;");
                builder.AddContent(10, "Select All");
                builder.CloseElement();

                builder.OpenElement(11, "button");
                builder.AddAttribute(12, "style", "padding:4px 12px; border:1px solid #d9d9d9; border-radius:2px; cursor:pointer;");
                builder.AddContent(13, "Deselect All");
                builder.CloseElement();
            }

            if (ShowExpandAll)
            {
                builder.OpenElement(14, "button");
                builder.AddAttribute(15, "style", "padding:4px 12px; border:1px solid #d9d9d9; border-radius:2px; cursor:pointer;");
                builder.AddContent(16, "Expand All");
                builder.CloseElement();

                builder.OpenElement(17, "button");
                builder.AddAttribute(18, "style", "padding:4px 12px; border:1px solid #d9d9d9; border-radius:2px; cursor:pointer;");
                builder.AddContent(19, "Collapse All");
                builder.CloseElement();
            }

            builder.CloseElement();
        }

        // 权限树容器
        builder.OpenElement(20, "div");
        builder.AddAttribute(21, "class", "permission-tree-container");
        builder.AddAttribute(22, "style", "min-height:300px; max-height:500px; overflow-y:auto; border:1px solid #e0e0e0; border-radius:2px; padding:12px; background:#fafafa;");

        // 示例权限节点(实际应从 /api/access/functions API 加载功能树数据)
        RenderRuntimePermissionNode(builder, "Customer Management", true, 0);
        builder.OpenElement(24, "div");
        builder.AddAttribute(25, "style", "margin-left:24px;");
        RenderRuntimePermissionNode(builder, "View Customers", true, 1);
        RenderRuntimePermissionNode(builder, "Edit Customers", false, 1);
        RenderRuntimePermissionNode(builder, "Delete Customers", false, 1);
        builder.CloseElement();

        RenderRuntimePermissionNode(builder, "Organization Management", false, 0);

        if (ShowTemplateBindings)
        {
            builder.OpenElement(30, "div");
            builder.AddAttribute(31, "style", "margin-left:24px;");
            RenderRuntimePermissionNode(builder, "Template: Customer Detail", true, 1);
            RenderRuntimePermissionNode(builder, "Template: Customer List", true, 1);
            builder.CloseElement();
        }

        builder.CloseElement(); // permission-tree-container

        // 配置信息提示
        builder.OpenElement(34, "div");
        builder.AddAttribute(35, "style", "margin-top:8px; font-size:11px; color:#999; display:flex; gap:12px;");
        builder.OpenElement(36, "span");
        builder.AddContent(37, $"Cascade: {CascadeSelect}");
        builder.CloseElement();
        builder.OpenElement(38, "span");
        builder.AddContent(39, $"Read-only: {ReadOnly}");
        builder.CloseElement();
        builder.OpenElement(40, "span");
        builder.AddContent(41, $"Show templates: {ShowTemplateBindings}");
        builder.CloseElement();
        builder.CloseElement();

        builder.CloseElement(); // container
    }

    private void RenderRuntimePermissionNode(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, string text, bool isChecked, int level)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", "padding:6px 8px; display:flex; align-items:center; gap:8px; border-radius:2px; hover:background:#e6f7ff;");

        if (!ReadOnly)
        {
            builder.OpenElement(2, "input");
            builder.AddAttribute(3, "type", "checkbox");
            builder.AddAttribute(4, "checked", isChecked);
            builder.AddAttribute(5, "style", "cursor:pointer;");
            builder.CloseElement();
        }

        if (ShowNodeIcons)
        {
            builder.OpenElement(6, "span");
            builder.AddAttribute(7, "style", "color:#1890ff;");
            builder.AddContent(8, level == 0 ? "📁" : "📄");
            builder.CloseElement();
        }

        builder.OpenElement(9, "span");
        builder.AddAttribute(10, "style", isChecked ? "font-weight:500; color:#000;" : "color:#666;");
        builder.AddContent(11, text);
        builder.CloseElement();

        builder.CloseElement();
    }

    public override void RenderDesign(DesignRenderContext context)
    {
        var builder = context.Builder;

        // 设计态预览
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"padding:8px; background:{context.BackgroundResolver(this)}; border:1px dashed #d9d9d9;");

        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "style", $"{context.TextStyleResolver(this)} font-weight:bold; margin-bottom:8px;");
        builder.AddContent(4, context.Localize(Label));
        builder.CloseElement();

        // 权限树占位符
        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "style", "padding-left:8px;");

        // 模拟权限节点
        RenderPermissionNodePlaceholder(builder, "Customer Management", true);
        builder.OpenElement(8, "div");
        builder.AddAttribute(9, "style", "padding-left:16px;");
        RenderPermissionNodePlaceholder(builder, "View Customers", true);
        RenderPermissionNodePlaceholder(builder, "Edit Customers", false);
        builder.CloseElement();

        RenderPermissionNodePlaceholder(builder, "Organization Management", false);

        builder.CloseElement();
        builder.CloseElement();
    }

    private void RenderPermissionNodePlaceholder(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, string text, bool checked_)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", "padding:4px 0; font-size:11px; color:#666; display:flex; align-items:center; gap:4px;");

        // 复选框
        builder.OpenElement(2, "input");
        builder.AddAttribute(3, "type", "checkbox");
        builder.AddAttribute(4, "disabled", true);
        if (checked_)
        {
            builder.AddAttribute(5, "checked", true);
        }
        builder.CloseElement();

        // 文本
        builder.OpenElement(6, "span");
        builder.AddContent(7, text);
        builder.CloseElement();

        builder.CloseElement();
    }
}
