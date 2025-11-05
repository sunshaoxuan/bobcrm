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
/// 默认的设计态控件渲染实现。集中处理所有非容器控件的预览。
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
        switch (widget)
        {
            case TextboxWidget:
                RenderTextbox(builder, widget, textStyleResolver, backgroundResolver, localize);
                break;
            case NumberWidget:
                RenderNumber(builder, widget, textStyleResolver, backgroundResolver);
                break;
            case SelectWidget:
                RenderSelect(builder, widget, textStyleResolver, backgroundResolver);
                break;
            case ListboxWidget:
                RenderListbox(builder, widget, textStyleResolver, backgroundResolver);
                break;
            case TextareaWidget:
                RenderTextarea(builder, widget, textStyleResolver, backgroundResolver);
                break;
            case CalendarWidget:
                RenderCalendar(builder, widget, textStyleResolver, backgroundResolver);
                break;
            case ButtonWidget button:
                RenderButton(builder, button, textStyleResolver, backgroundResolver);
                break;
            case LabelWidget:
                RenderLabel(builder, widget, textStyleResolver, backgroundResolver);
                break;
            default:
                RenderFallback(builder, widget, textStyleResolver, backgroundResolver);
                break;
        }
    };

    private static void RenderTextbox(RenderTreeBuilder builder, DraggableWidget widget, Func<DraggableWidget, string> textStyleResolver, Func<DraggableWidget, string> backgroundResolver, Func<string, string> localize)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"padding:6px; background:{backgroundResolver(widget)}; pointer-events:none;");
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "style", $"{textStyleResolver(widget)} font-size:11px; margin-bottom:2px;");
        builder.AddContent(4, widget.Label);
        builder.CloseElement();
        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "style", "height:32px; background:#fff; border:1px solid #e0e0e0; border-radius:2px; display:flex; align-items:center; justify-content:space-between; padding:0 8px;");
        builder.OpenElement(7, "span");
        builder.AddAttribute(8, "style", "font-size:12px; color:#999;");
        builder.AddContent(9, localize("LBL_PLACEHOLDER_TEXTBOX"));
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    }

    private static void RenderNumber(RenderTreeBuilder builder, DraggableWidget widget, Func<DraggableWidget, string> textStyleResolver, Func<DraggableWidget, string> backgroundResolver)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"padding:6px; background:{backgroundResolver(widget)}; pointer-events:none;");
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "style", $"{textStyleResolver(widget)} font-size:11px; margin-bottom:2px;");
        builder.AddContent(4, widget.Label);
        builder.CloseElement();
        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "style", "height:32px; background:#fff; border:1px solid #e0e0e0; border-radius:2px; display:flex; align-items:center; justify-content:space-between; padding:0 6px; font-size:12px; color:#666;");
        builder.OpenElement(7, "span");
        builder.AddContent(8, "-");
        builder.CloseElement();
        builder.OpenElement(9, "span");
        builder.AddContent(10, "123");
        builder.CloseElement();
        builder.OpenElement(11, "span");
        builder.AddContent(12, "+");
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    }

    private static void RenderSelect(RenderTreeBuilder builder, DraggableWidget widget, Func<DraggableWidget, string> textStyleResolver, Func<DraggableWidget, string> backgroundResolver)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"padding:6px; background:{backgroundResolver(widget)}; pointer-events:none;");
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "style", $"{textStyleResolver(widget)} font-size:11px; margin-bottom:2px;");
        builder.AddContent(4, widget.Label);
        builder.CloseElement();
        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "style", "height:32px; background:#fff; border:1px solid #e0e0e0; border-radius:2px; display:flex; align-items:center; justify-content:space-between; padding:0 6px;");
        builder.OpenElement(7, "span");
        builder.AddAttribute(8, "style", "font-size:12px; color:#999;");
        builder.AddContent(9, "Option");
        builder.CloseElement();
        builder.OpenElement(10, "span");
        builder.AddAttribute(11, "style", "font-size:10px; color:#999;");
        builder.AddContent(12, "▼");
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    }

    private static void RenderListbox(RenderTreeBuilder builder, DraggableWidget widget, Func<DraggableWidget, string> textStyleResolver, Func<DraggableWidget, string> backgroundResolver)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"padding:6px; background:{backgroundResolver(widget)}; pointer-events:none;");
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "style", $"{textStyleResolver(widget)} font-size:11px; margin-bottom:2px;");
        builder.AddContent(4, widget.Label);
        builder.CloseElement();
        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "style", "height:32px; background:#fff; border:1px solid #e0e0e0; border-radius:2px; display:flex; align-items:center; padding:0 4px;");
        builder.OpenElement(7, "span");
        builder.AddAttribute(8, "style", "font-size:10px; color:#999;");
        builder.AddContent(9, "▼");
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    }

    private static void RenderTextarea(RenderTreeBuilder builder, DraggableWidget widget, Func<DraggableWidget, string> textStyleResolver, Func<DraggableWidget, string> backgroundResolver)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"padding:6px; background:{backgroundResolver(widget)}; pointer-events:none;");
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "style", $"{textStyleResolver(widget)} font-size:11px; margin-bottom:2px;");
        builder.AddContent(4, widget.Label);
        builder.CloseElement();
        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "style", "min-height:56px; background:#fff; border:1px solid #e0e0e0; border-radius:2px;");
        builder.CloseElement();
        builder.CloseElement();
    }

    private static void RenderCalendar(RenderTreeBuilder builder, DraggableWidget widget, Func<DraggableWidget, string> textStyleResolver, Func<DraggableWidget, string> backgroundResolver)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"padding:6px; background:{backgroundResolver(widget)}; pointer-events:none;");
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "style", $"{textStyleResolver(widget)} font-size:11px; margin-bottom:2px;");
        builder.AddContent(4, widget.Label);
        builder.CloseElement();
        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "style", "height:32px; background:#fff; border:1px solid #e0e0e0; border-radius:2px; display:flex; align-items:center; padding:0 4px;");
        builder.OpenElement(7, "span");
        builder.AddAttribute(8, "style", "font-size:10px; color:#999;");
        builder.AddContent(9, "📅");
        builder.CloseElement();
        builder.CloseElement();
        builder.CloseElement();
    }

    private static void RenderButton(RenderTreeBuilder builder, ButtonWidget widget, Func<DraggableWidget, string> textStyleResolver, Func<DraggableWidget, string> backgroundResolver)
    {
        var buttonColor = widget.Variant == "primary" ? "#1890ff" : "#f0f0f0";
        var textColor = widget.Variant == "primary" ? "#fff" : "#555";

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"padding:6px; background:{backgroundResolver(widget)}; pointer-events:none;");
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "style", $"display:inline-block; padding:6px 16px; border-radius:4px; background:{buttonColor}; color:{textColor}; font-size:12px;");
        builder.AddContent(4, widget.Label);
        builder.CloseElement();
        builder.CloseElement();
    }

    private static void RenderLabel(RenderTreeBuilder builder, DraggableWidget widget, Func<DraggableWidget, string> textStyleResolver, Func<DraggableWidget, string> backgroundResolver)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"padding:6px; background:{backgroundResolver(widget)}; pointer-events:none;");
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "style", $"{textStyleResolver(widget)} font-size:11px; font-weight:500;");
        builder.AddContent(4, widget.Label);
        builder.CloseElement();
        builder.CloseElement();
    }

    private static void RenderFallback(RenderTreeBuilder builder, DraggableWidget widget, Func<DraggableWidget, string> textStyleResolver, Func<DraggableWidget, string> backgroundResolver)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"padding:6px; background:{backgroundResolver(widget)}; pointer-events:none;");
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "style", $"{textStyleResolver(widget)} font-size:11px; margin-bottom:2px;");
        builder.AddContent(4, widget.Label);
        builder.CloseElement();
        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "style", "height:32px; background:#fff; border:1px solid #e0e0e0; border-radius:2px;");
        builder.CloseElement();
        builder.CloseElement();
    }
}
