using System.Threading.Tasks;
using BobCrm.App.Models.Widgets;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BobCrm.App.Services.Widgets.Rendering;

public enum RuntimeWidgetRenderMode
{
    Browse,
    Edit
}

public interface IRuntimeWidgetRenderer
{
    RenderFragment Render(RuntimeWidgetRenderRequest request);
}

public sealed record RuntimeWidgetRenderRequest
{
    public required DraggableWidget Widget { get; init; }
    public required RuntimeWidgetRenderMode Mode { get; init; }
    public required ComponentBase EventTarget { get; init; }
    public required string Label { get; init; }
    public Func<string>? ValueGetter { get; init; }
    public Func<string?, Task>? ValueSetter { get; init; }
    public Func<DraggableWidget, string>? GetWidgetTextStyle { get; init; }
    public Func<DraggableWidget, string>? GetWidgetBackground { get; init; }
    public IReadOnlyList<ListItem>? Items { get; init; }
}

public sealed class RuntimeWidgetRenderer : IRuntimeWidgetRenderer
{
    private readonly EventCallbackFactory _callbackFactory = new();

    public RenderFragment Render(RuntimeWidgetRenderRequest request) => builder =>
    {
        switch (request.Widget)
        {
            case LabelWidget:
                RenderLabel(builder, request);
                break;
            case TextboxWidget textbox:
                RenderTextbox(builder, request, textbox);
                break;
            case NumberWidget number:
                RenderNumber(builder, request, number);
                break;
            case TextareaWidget textarea:
                RenderTextarea(builder, request, textarea);
                break;
            case CalendarWidget calendar:
                RenderCalendar(builder, request, calendar);
                break;
            case SelectWidget select:
                RenderSelect(builder, request, select);
                break;
            case ListboxWidget listbox:
                RenderSelect(builder, request with { Items = listbox.Items }, listbox);
                break;
            case ButtonWidget button:
                RenderButton(builder, request, button);
                break;
            default:
                RenderFallback(builder, request);
                break;
        }
    };

    private static string ResolveTextStyle(RuntimeWidgetRenderRequest request)
        => request.GetWidgetTextStyle?.Invoke(request.Widget) ?? "font-size:12px; color:#333;";

    private static string ResolveBackground(RuntimeWidgetRenderRequest request)
        => request.GetWidgetBackground?.Invoke(request.Widget) ?? "#fff";

    private void RenderLabel(RenderTreeBuilder builder, RuntimeWidgetRenderRequest request)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"{ResolveTextStyle(request)} font-weight:600;");
        builder.AddContent(2, request.Label);
        builder.CloseElement();
    }

    private void RenderTextbox(RenderTreeBuilder builder, RuntimeWidgetRenderRequest request, TextboxWidget widget)
    {
        var value = request.ValueGetter?.Invoke() ?? string.Empty;
        if (request.Mode == RuntimeWidgetRenderMode.Edit)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "style", "display:flex; flex-direction:column; gap:6px;");
            RenderFieldLabel(builder, request);
            builder.OpenElement(4, "input");
            builder.AddAttribute(5, "class", "runtime-field-input");
            builder.AddAttribute(6, "value", value);
            builder.AddAttribute(7, "type", widget.Readonly ? "text" : "text");
            if (!string.IsNullOrWhiteSpace(widget.Placeholder))
            {
                builder.AddAttribute(8, "placeholder", widget.Placeholder);
            }
            if (widget.MaxLength.HasValue)
            {
                builder.AddAttribute(9, "maxlength", widget.MaxLength.Value);
            }
            if (widget.Readonly)
            {
                builder.AddAttribute(10, "readonly", true);
            }
            if (request.ValueSetter != null)
            {
                builder.AddAttribute(11, "oninput",
                    _callbackFactory.Create<ChangeEventArgs>(request.EventTarget,
                        e => request.ValueSetter!(e.Value?.ToString())));
            }
            builder.CloseElement(); // input
            builder.CloseElement(); // container
        }
        else
        {
            RenderReadOnlyValue(builder, request, value);
        }
    }

    private void RenderNumber(RenderTreeBuilder builder, RuntimeWidgetRenderRequest request, NumberWidget widget)
    {
        var value = request.ValueGetter?.Invoke() ?? string.Empty;
        if (request.Mode == RuntimeWidgetRenderMode.Edit)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "style", "display:flex; flex-direction:column; gap:6px;");
            RenderFieldLabel(builder, request);
            builder.OpenElement(4, "input");
            builder.AddAttribute(5, "class", "runtime-field-input");
            builder.AddAttribute(6, "type", widget.AllowDecimal ? "number" : "number");
            builder.AddAttribute(7, "step", widget.AllowDecimal ? widget.Step.ToString() : "1");
            if (widget.MinValue.HasValue)
            {
                builder.AddAttribute(8, "min", widget.MinValue.Value);
            }
            if (widget.MaxValue.HasValue)
            {
                builder.AddAttribute(9, "max", widget.MaxValue.Value);
            }
            builder.AddAttribute(10, "value", value);
            if (request.ValueSetter != null)
            {
                builder.AddAttribute(11, "oninput",
                    _callbackFactory.Create<ChangeEventArgs>(request.EventTarget,
                        e => request.ValueSetter!(e.Value?.ToString())));
            }
            builder.CloseElement();
            builder.CloseElement();
        }
        else
        {
            RenderReadOnlyValue(builder, request, value);
        }
    }

    private void RenderTextarea(RenderTreeBuilder builder, RuntimeWidgetRenderRequest request, TextareaWidget widget)
    {
        var value = request.ValueGetter?.Invoke() ?? string.Empty;
        if (request.Mode == RuntimeWidgetRenderMode.Edit)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "style", "display:flex; flex-direction:column; gap:6px;");
            RenderFieldLabel(builder, request);
            builder.OpenElement(4, "textarea");
            builder.AddAttribute(5, "class", "runtime-field-input");
            builder.AddAttribute(6, "style", "min-height:80px; resize:vertical;");
            builder.AddContent(7, value);
            if (request.ValueSetter != null)
            {
                builder.AddAttribute(8, "oninput",
                    _callbackFactory.Create<ChangeEventArgs>(request.EventTarget,
                        e => request.ValueSetter!(e.Value?.ToString())));
            }
            builder.CloseElement();
            builder.CloseElement();
        }
        else
        {
            RenderReadOnlyValue(builder, request, value);
        }
    }

    private void RenderCalendar(RenderTreeBuilder builder, RuntimeWidgetRenderRequest request, CalendarWidget widget)
    {
        var value = request.ValueGetter?.Invoke() ?? string.Empty;
        if (DateTime.TryParse(value, out var dt))
        {
            value = dt.ToString("yyyy-MM-dd");
        }

        if (request.Mode == RuntimeWidgetRenderMode.Edit)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "style", "display:flex; flex-direction:column; gap:6px;");
            RenderFieldLabel(builder, request);
            builder.OpenElement(4, "input");
            builder.AddAttribute(5, "class", "runtime-field-input");
            builder.AddAttribute(6, "type", "date");
            builder.AddAttribute(7, "value", value);
            if (request.ValueSetter != null)
            {
                builder.AddAttribute(8, "onchange",
                    _callbackFactory.Create<ChangeEventArgs>(request.EventTarget,
                        e => request.ValueSetter!(e.Value?.ToString())));
            }
            builder.CloseElement();
            builder.CloseElement();
        }
        else
        {
            RenderReadOnlyValue(builder, request, value);
        }
    }

    private void RenderSelect(RenderTreeBuilder builder, RuntimeWidgetRenderRequest request, TextWidget widget)
    {
        var items = request.Items ?? (widget is SelectWidget select ? select.Items : Array.Empty<ListItem>());
        var value = request.ValueGetter?.Invoke() ?? string.Empty;
        var display = items.FirstOrDefault(i => i.Value == value)?.Label ?? value;

        if (request.Mode == RuntimeWidgetRenderMode.Edit)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "style", "display:flex; flex-direction:column; gap:6px;");
            RenderFieldLabel(builder, request);
            builder.OpenElement(4, "select");
            builder.AddAttribute(5, "class", "runtime-field-input");
            if (request.ValueSetter != null)
            {
                builder.AddAttribute(6, "onchange",
                    _callbackFactory.Create<ChangeEventArgs>(request.EventTarget,
                        e => request.ValueSetter!(e.Value?.ToString())));
            }

            builder.OpenElement(7, "option");
            builder.AddAttribute(8, "value", string.Empty);
            builder.AddContent(9, "--");
            builder.CloseElement();

            foreach (var item in items)
            {
                builder.OpenElement(10, "option");
                builder.AddAttribute(11, "value", item.Value);
                if (item.Value == value)
                {
                    builder.AddAttribute(12, "selected", true);
                }
                builder.AddContent(13, item.Label ?? item.Value);
                builder.CloseElement();
            }

            builder.CloseElement(); // select
            builder.CloseElement(); // container
        }
        else
        {
            RenderReadOnlyValue(builder, request, display);
        }
    }

    private void RenderButton(RenderTreeBuilder builder, RuntimeWidgetRenderRequest request, ButtonWidget widget)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", "display:flex; flex-direction:column; gap:6px;");
        RenderFieldLabel(builder, request);
        builder.OpenElement(4, "button");
        builder.AddAttribute(5, "type", "button");
        builder.AddAttribute(6, "style", "padding:6px 16px; border-radius:4px; border:none; cursor:pointer; background:#1890ff; color:#fff;");
        if (request.Mode == RuntimeWidgetRenderMode.Browse)
        {
            builder.AddAttribute(7, "disabled", true);
        }
        builder.AddContent(8, widget.Label);
        builder.CloseElement();
        builder.CloseElement();
    }

    private void RenderFallback(RenderTreeBuilder builder, RuntimeWidgetRenderRequest request)
    {
        var value = request.ValueGetter?.Invoke() ?? string.Empty;
        if (request.Mode == RuntimeWidgetRenderMode.Edit && request.ValueSetter != null)
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "style", "display:flex; flex-direction:column; gap:6px;");
            RenderFieldLabel(builder, request);
            builder.OpenElement(4, "input");
            builder.AddAttribute(5, "class", "runtime-field-input");
            builder.AddAttribute(6, "value", value);
            builder.AddAttribute(7, "oninput", _callbackFactory.Create<ChangeEventArgs>(request.EventTarget,
                e => request.ValueSetter!(e.Value?.ToString())));
            builder.CloseElement();
            builder.CloseElement();
        }
        else
        {
            RenderReadOnlyValue(builder, request, value);
        }
    }

    private static void RenderFieldLabel(RenderTreeBuilder builder, RuntimeWidgetRenderRequest request)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "runtime-field-label");
        builder.AddContent(2, request.Label);
        builder.CloseElement();
    }

    private static void RenderReadOnlyValue(RenderTreeBuilder builder, RuntimeWidgetRenderRequest request, string value)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"display:flex; flex-direction:column; gap:4px; background:{ResolveBackground(request)}; padding:4px 0;");
        RenderFieldLabel(builder, request);
        builder.OpenElement(4, "div");
        builder.AddAttribute(5, "class", "runtime-field-value");
        builder.AddAttribute(6, "style", ResolveTextStyle(request));
        builder.AddContent(7, string.IsNullOrWhiteSpace(value) ? "--" : value);
        builder.CloseElement();
        builder.CloseElement();
    }
}
