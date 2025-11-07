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
        Func<DraggableWidget, string> textStyleResolver,
        Func<DraggableWidget, string> backgroundResolver,
        Func<string, string> localize)
        => builder =>
    {
        // 使用多态：每个Widget自己负责渲染
        var context = new DraggableWidget.DesignRenderContext
        {
            Builder = builder,
            TextStyleResolver = textStyleResolver,
            BackgroundResolver = backgroundResolver,
            Localize = localize
        };

        widget.RenderDesign(context);
    };
}
