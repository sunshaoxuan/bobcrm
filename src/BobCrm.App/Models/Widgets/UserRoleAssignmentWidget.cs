using System.Collections.Generic;
using BobCrm.App.Models.Designer;
using Microsoft.AspNetCore.Components;
using AntDesign;
using BobCrm.App.Services.Widgets;

namespace BobCrm.App.Models.Widgets;

[WidgetMetadata("userrole", "LBL_USERROLE", "Outline.UserSwitch", WidgetCategory.Data)]
/// <summary>
/// 用户角色分配控件 - 显示和编辑用户的角色分配
/// </summary>
public class UserRoleAssignmentWidget : DraggableWidget
{
    /// <summary>
    /// 用户ID字段(用于绑定数据)
    /// </summary>
    public string? UserIdField { get; set; }

    /// <summary>
    /// 是否显示搜索框
    /// </summary>
    public bool ShowSearch { get; set; } = true;

    /// <summary>
    /// 是否显示全选按钮
    /// </summary>
    public bool ShowSelectAll { get; set; } = true;

    /// <summary>
    /// 是否显示当前角色列表
    /// </summary>
    public bool ShowCurrentRoles { get; set; } = true;

    /// <summary>
    /// 是否只读模式(仅查看,不可编辑)
    /// </summary>
    public bool ReadOnly { get; set; } = false;

    /// <summary>
    /// 角色选择变化时的回调动作(预留,用于行为脚本)
    /// </summary>
    public string? OnRoleChangeAction { get; set; }

    /// <summary>
    /// 是否显示组织范围标签
    /// </summary>
    public bool ShowOrganizationScope { get; set; } = true;

    public override string GetDefaultCodePrefix() => "userrole";

    public override List<WidgetPropertyMetadata> GetPropertyMetadata()
    {
        var props = base.GetPropertyMetadata();

        props.AddRange(new[]
        {
            new WidgetPropertyMetadata
            {
                PropertyPath = "UserIdField",
                Label = "PROP_USER_ID_FIELD",
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
                PropertyPath = "ShowCurrentRoles",
                Label = "PROP_SHOW_CURRENT_ROLES",
                EditorType = PropertyEditorType.Boolean,
                Group = "PROP_GROUP_DISPLAY"
            },
            new WidgetPropertyMetadata
            {
                PropertyPath = "ShowOrganizationScope",
                Label = "PROP_SHOW_ORGANIZATION_SCOPE",
                EditorType = PropertyEditorType.Boolean,
                Group = "PROP_GROUP_DISPLAY"
            },
            new WidgetPropertyMetadata
            {
                PropertyPath = "ReadOnly",
                Label = "PROP_READ_ONLY",
                EditorType = PropertyEditorType.Boolean,
                Group = "PROP_GROUP_BEHAVIOR"
            }
        });

        return props;
    }

    public override void RenderRuntime(RuntimeRenderContext context)
    {
        var builder = context.Builder;

        // 运行态渲染 - 渲染实际的 UserRoleAssignment 组件
        builder.OpenComponent(0, typeof(BobCrm.App.Components.Shared.UserRoleAssignment));

        // 绑定 UserId 参数 - 从实体 ID 获取
        if (context.EventTarget is ComponentBase component)
        {
            // 尝试从上下文获取实体 ID
            var idProperty = component.GetType().GetProperty("Id");
            if (idProperty != null)
            {
                var entityId = idProperty.GetValue(component);
                if (entityId != null)
                {
                    builder.AddAttribute(1, "UserId", entityId.ToString());
                }
            }
        }

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

        // 用户角色分配占位符
        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "style", "padding-left:8px;");

        // 模拟穿梭框
        builder.OpenElement(7, "div");
        builder.AddAttribute(8, "style", "display:flex; gap:16px; align-items:center;");

        // 左侧列表
        RenderRoleListPlaceholder(builder, "Available Roles", new[] { "Admin", "Manager", "User" });

        // 箭头
        builder.OpenElement(12, "div");
        builder.AddAttribute(13, "style", "display:flex; flex-direction:column; gap:4px;");
        builder.OpenElement(14, "span");
        builder.AddAttribute(15, "style", "font-size:18px;");
        builder.AddContent(16, "→");
        builder.CloseElement();
        builder.OpenElement(17, "span");
        builder.AddAttribute(18, "style", "font-size:18px;");
        builder.AddContent(19, "←");
        builder.CloseElement();
        builder.CloseElement();

        // 右侧列表
        RenderRoleListPlaceholder(builder, "Assigned Roles", new[] { "User" });

        builder.CloseElement();

        // 配置信息
        builder.OpenElement(20, "div");
        builder.AddAttribute(21, "style", "margin-top:8px; font-size:11px; color:#999;");
        builder.AddContent(22, $"Search: {ShowSearch}, Read-only: {ReadOnly}");
        builder.CloseElement();

        builder.CloseElement();
        builder.CloseElement();
    }

    private void RenderRoleListPlaceholder(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder, string title, string[] roles)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", "flex:1; border:1px solid #d9d9d9; border-radius:4px; padding:8px;");

        // 标题
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "style", "font-weight:600; margin-bottom:8px; font-size:12px;");
        builder.AddContent(4, title);
        builder.CloseElement();

        // 角色列表
        foreach (var role in roles)
        {
            builder.OpenElement(5, "div");
            builder.AddAttribute(6, "style", "padding:4px 8px; background:#fafafa; margin-bottom:4px; border-radius:2px; font-size:11px;");
            builder.AddContent(7, role);
            builder.CloseElement();
        }

        builder.CloseElement();
    }
}
