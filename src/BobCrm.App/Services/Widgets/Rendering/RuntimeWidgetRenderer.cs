using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BobCrm.App.Models;
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
    public required FormRuntimeContext FormContext { get; init; }
    public required ComponentBase EventTarget { get; init; }
    public required string Label { get; init; }
    public Func<string>? ValueGetter { get; init; }
    public Func<string?, Task>? ValueSetter { get; init; }
    public Func<DraggableWidget, string>? GetWidgetTextStyle { get; init; }
    public Func<DraggableWidget, string>? GetWidgetBackground { get; init; }
    public IReadOnlyList<ListItem>? Items { get; init; }
}

/// <summary>
/// 运行时组件渲染器 - 通过 DynamicComponent 驱动渲染
/// </summary>
public sealed class RuntimeWidgetRenderer : IRuntimeWidgetRenderer
{
    public RenderFragment Render(RuntimeWidgetRenderRequest request) => builder =>
    {
        var componentType = request.Widget.RuntimeComponentType
            ?? typeof(BobCrm.App.Components.Widgets.DefaultTextComponent);

        builder.OpenComponent<CascadingValue<FormRuntimeContext>>(0);
        builder.AddAttribute(1, "Value", request.FormContext);
        builder.AddAttribute(2, "IsFixed", false);
        builder.AddAttribute(3, "ChildContent", (RenderFragment)(childBuilder =>
        {
            childBuilder.OpenComponent<DynamicComponent>(0);
            childBuilder.AddAttribute(1, "Type", componentType);

            // 仅 DefaultTextComponent 需要旧的 RuntimeRenderContext 参数；其余运行时组件只接收 Widget（Context 走级联）。
            var parameters = componentType == typeof(BobCrm.App.Components.Widgets.DefaultTextComponent)
                ? new Dictionary<string, object?>
                {
                    ["Widget"] = request.Widget,
                    ["Mode"] = request.Mode,
                    ["EventTarget"] = request.EventTarget,
                    ["Label"] = request.Label,
                    ["RenderChild"] = (Func<DraggableWidget, RenderFragment>)(childWidget =>
                        Render(request with { Widget = childWidget, Label = childWidget.Label ?? childWidget.Type })),
                    ["ValueGetter"] = request.ValueGetter,
                    ["ValueSetter"] = request.ValueSetter,
                    ["GetWidgetTextStyle"] = request.GetWidgetTextStyle,
                    ["GetWidgetBackground"] = request.GetWidgetBackground,
                    ["Items"] = request.Items
                }
                : new Dictionary<string, object?>
                {
                    ["Widget"] = request.Widget
                };

            childBuilder.AddAttribute(2, "Parameters", parameters);
            childBuilder.CloseComponent();
        }));
        builder.CloseComponent();
    };
}
