using System;
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

        // 运行态渲染 - 渲染实际的 RolePermissionTree 组件
        builder.OpenComponent(0, typeof(BobCrm.App.Components.Shared.RolePermissionTree));

        // 绑定 RoleId 参数 - 从实体 ID 获取
        if (context.EventTarget is Microsoft.AspNetCore.Components.ComponentBase component)
        {
            // 尝试从上下文获取实体 ID (RoleProfile 的 Id)
            var idProperty = component.GetType().GetProperty("Id");
            if (idProperty != null)
            {
                var entityId = idProperty.GetValue(component);
                if (entityId != null && Guid.TryParse(entityId.ToString(), out var roleId))
                {
                    builder.AddAttribute(1, "RoleId", roleId);
                }
            }
        }

        // 传递其他配置参数
        builder.AddAttribute(2, "ShowSearch", ShowSearch);
        builder.AddAttribute(3, "ShowStatistics", ShowStatistics);
        builder.AddAttribute(4, "DefaultExpandAll", DefaultExpandLevel == -1);
        builder.AddAttribute(5, "CascadeSelect", CascadeSelect);

        builder.CloseComponent();
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
