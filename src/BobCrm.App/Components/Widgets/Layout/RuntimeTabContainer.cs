using BobCrm.App.Models.Widgets;
using BobCrm.App.Services.Widgets;
using BobCrm.App.Services.Widgets.Rendering;
using BobCrm.App.Models;
using BobCrm.App.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BobCrm.App.Components.Widgets.Layout;

/// <summary>
/// TabContainer 运行时组件
/// </summary>
public class RuntimeTabContainer : ComponentBase
{
    [Parameter] public DraggableWidget Widget { get; set; } = default!;

    [CascadingParameter] public FormRuntimeContext FormContext { get; set; } = default!;

    [Inject] public IRuntimeWidgetRenderer RuntimeRenderer { get; set; } = default!;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Widget is not TabContainerWidget tabContainer)
        {
            builder.OpenElement(0, "div");
            builder.AddContent(1, "Widget is not TabContainerWidget");
            builder.CloseElement();
            return;
        }

        var mode = FormContext.RenderMode == FormRuntimeContext.Mode.Edit 
            ? RuntimeWidgetRenderMode.Edit 
            : RuntimeWidgetRenderMode.Browse;

        RuntimeContainerRenderer.RenderTabContainer(
            builder,
            tabContainer,
            mode,
            (child, m) => RuntimeRenderer.Render(new RuntimeWidgetRenderRequest
            {
                Widget = child,
                Mode = m,
                FormContext = FormContext,
                EventTarget = this,
                Label = child.Label ?? child.Type,
                // 为了兼容 RuntimeWidgetRenderer 的参数需求，我们需要构建 ValueGetter/Setter。
                // 此时的数据源是 FormContext.Data。
                // 如果是编辑模式，数据源是 EditValueManager；如果是浏览模式，数据源可能是 Dictionary。
                ValueGetter = () => 
                {
                    var key = child.DataField ?? child.Id;
                    if (FormContext.Data is EditValueManager evm) return evm.GetValue(key);
                    if (FormContext.Data is IReadOnlyDictionary<string, object?> dict && dict.TryGetValue(key, out var val)) return val?.ToString();
                    return null;
                },
                ValueSetter = val =>
                {
                    if (FormContext.Data is EditValueManager evm)
                    {
                        evm.SetValue(child.DataField ?? child.Id, val);
                    }
                    return Task.CompletedTask;
                }
            }),
            container => 
            {
                var tabs = container.Children?.OfType<TabWidget>().ToList();
                if (tabs == null || !tabs.Any()) return null;
                var active = tabs.FirstOrDefault(t => t.TabId == container.ActiveTabId) 
                             ?? tabs.FirstOrDefault(t => t.IsDefault) 
                             ?? tabs.First();
                return active;
            },
            (w, m) => WidgetStyleHelper.GetRuntimeWidgetStyle(w, m)
        );
    }
}
