using System.Threading.Tasks;
using BobCrm.App.Models.Widgets;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BobCrm.App.Services.Widgets.Rendering;

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

/// <summary>
/// 运行时组件渲染器 - 使用OOP多态模式委托给各组件自己的渲染方法
/// </summary>
public sealed class RuntimeWidgetRenderer : IRuntimeWidgetRenderer
{
    public RenderFragment Render(RuntimeWidgetRenderRequest request) => builder =>
    {
        // 使用多态：每个Widget自己负责渲染
        var context = new DraggableWidget.RuntimeRenderContext
        {
            Builder = builder,
            Mode = request.Mode,
            EventTarget = request.EventTarget,
            Widget = request.Widget,
            Label = request.Label,
            ValueGetter = request.ValueGetter,
            ValueSetter = request.ValueSetter,
            GetWidgetTextStyle = request.GetWidgetTextStyle,
            GetWidgetBackground = request.GetWidgetBackground,
            Items = request.Items
        };

        request.Widget.RenderRuntime(context);
    };
}
