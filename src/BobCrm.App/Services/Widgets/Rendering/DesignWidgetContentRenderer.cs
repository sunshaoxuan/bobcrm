using System.Collections.Generic;
using BobCrm.App.Models;
using BobCrm.App.Models.Widgets;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

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
            childBuilder.OpenComponent<DynamicComponent>(0);
            childBuilder.AddAttribute(1, "Type", componentType);
            childBuilder.AddAttribute(2, "Parameters", new Dictionary<string, object?> { ["Widget"] = widget });
            childBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    };
}
