using System.Collections.Generic;
using BobCrm.App.Models;
using BobCrm.App.Models.Widgets;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BobCrm.App.Services.Widgets.Rendering;

/// <summary>
/// 提供设计态控件内容渲染（不含外层拖拽包装与事件绑定）。
/// </summary>
public interface IDesignWidgetContentRenderer
{
    RenderFragment Render(
        DraggableWidget widget,
        FormRuntimeContext formContext,
        Func<DraggableWidget, string> textStyleResolver,
        Func<DraggableWidget, string> backgroundResolver,
        Func<string, string> localize);
}

/// <summary>
/// 设计态控件渲染器 - 使用OOP多态模式委托给各组件自己的渲染方法
/// </summary>
public sealed class DesignWidgetContentRenderer : IDesignWidgetContentRenderer
{
    public RenderFragment Render(
        DraggableWidget widget,
        FormRuntimeContext formContext,
        Func<DraggableWidget, string> textStyleResolver,
        Func<DraggableWidget, string> backgroundResolver,
        Func<string, string> localize)
        => builder =>
    {
        var componentType = widget.PreviewComponentType
            ?? typeof(BobCrm.App.Components.Widgets.DefaultTextComponent);

        builder.OpenComponent<CascadingValue<FormRuntimeContext>>(0);
        builder.AddAttribute(1, "Value", formContext);
        builder.AddAttribute(2, "IsFixed", false);
        builder.AddAttribute(3, "ChildContent", (RenderFragment)(childBuilder =>
        {
            childBuilder.OpenComponent<ErrorBoundary>(0);
            childBuilder.AddAttribute(1, "ChildContent", (RenderFragment)(safeBuilder =>
            {
                safeBuilder.OpenComponent<DynamicComponent>(0);
                safeBuilder.AddAttribute(1, "Type", componentType);
                safeBuilder.AddAttribute(2, "Parameters", new Dictionary<string, object?> { ["Widget"] = widget });
                safeBuilder.CloseComponent();
            }));
            childBuilder.AddAttribute(2, "ErrorContent", (RenderFragment<System.Exception>)(ex => errorBuilder =>
            {
                errorBuilder.OpenElement(0, "div");
                errorBuilder.AddAttribute(1, "style", "padding:10px; border:1px solid #ffccc7; background:#fff2f0; border-radius:6px; color:#a8071a; font-size:12px;");
                errorBuilder.AddContent(2, $"Widget preview failed: {widget.Type}");
                if (!string.IsNullOrWhiteSpace(ex.Message))
                {
                    errorBuilder.OpenElement(3, "div");
                    errorBuilder.AddAttribute(4, "style", "margin-top:6px; color:#555;");
                    errorBuilder.AddContent(5, ex.Message);
                    errorBuilder.CloseElement();
                }
                errorBuilder.CloseElement();
            }));
            childBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    };
}
