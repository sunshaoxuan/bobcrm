using BobCrm.App.Models.Widgets;
using BobCrm.App.Services.Widgets.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BobCrm.App.Services.Widgets.Rendering;

/// <summary>
/// 运行态容器渲染助手
/// 负责渲染Container和TabContainer的运行态视图
/// </summary>
public static class RuntimeContainerRenderer
{
    /// <summary>
    /// 渲染普通容器（Frame, Section, Panel等）
    /// </summary>
    public static void RenderContainer(
        RenderTreeBuilder builder,
        ContainerWidget container,
        RuntimeWidgetRenderMode mode,
        Func<DraggableWidget, RuntimeWidgetRenderMode, RenderFragment> renderChild,
        Func<DraggableWidget, string> labelGetter,
        Func<DraggableWidget, RuntimeWidgetRenderMode, string> styleGetter)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "runtime-container");

        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "class", "runtime-container-header");
        builder.AddContent(4, labelGetter(container));
        builder.CloseElement();

        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "class", "runtime-container-body");

        if (container.Children != null)
        {
            foreach (var child in container.Children.Where(c => c.Visible))
            {
                if (child.NewLine)
                {
                    builder.OpenElement(7, "div");
                    builder.AddAttribute(8, "class", "runtime-flow-break");
                    builder.CloseElement();
                }

                builder.OpenElement(9, "div");
                builder.AddAttribute(10, "data-widget-id", child.Id);
                builder.AddAttribute(11, "style", styleGetter(child, mode));
                builder.AddContent(12, renderChild(child, mode));
                builder.CloseElement();
            }
        }

        builder.CloseElement(); // body
        builder.CloseElement(); // container
    }

    /// <summary>
    /// 渲染TabContainer
    /// </summary>
    public static void RenderTabContainer(
        RenderTreeBuilder builder,
        TabContainerWidget tabContainer,
        RuntimeWidgetRenderMode mode,
        Func<DraggableWidget, RuntimeWidgetRenderMode, RenderFragment> renderChild,
        Func<TabContainerWidget, TabWidget?> getActiveTab,
        Func<DraggableWidget, RuntimeWidgetRenderMode, string> styleGetter)
    {
        var tabs = tabContainer.Children?.OfType<TabWidget>().Where(t => t.Visible).ToList() ?? new List<TabWidget>();
        var activeTab = getActiveTab(tabContainer);

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "runtime-tab-container");
        builder.AddAttribute(2, "style", "display:flex; flex-direction:column; width:100%; min-height:100px;");
        builder.AddAttribute(3, "data-debug-active-tab", activeTab?.TabId ?? "null");
        builder.AddAttribute(4, "data-debug-tabs-count", tabs.Count);
        builder.AddAttribute(5, "data-debug-total-children", tabContainer.Children?.Count ?? 0);
        builder.AddAttribute(6, "data-debug-tabs-filtered", tabs.Count);

        // Tab headers
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "class", "runtime-tab-headers");
        foreach (var tab in tabs)
        {
            var isActive = activeTab?.TabId == tab.TabId;
            var headerClass = isActive ? "runtime-tab-header active" : "runtime-tab-header";
            builder.OpenElement(4, "div");
            builder.AddAttribute(5, "class", headerClass);
            builder.AddContent(6, tab.Label);
            builder.CloseElement();
        }
        builder.CloseElement(); // headers

        // Active tab body
        if (activeTab?.Children != null)
        {
            builder.OpenElement(7, "div");
            builder.AddAttribute(8, "class", "runtime-tab-body");

            foreach (var child in activeTab.Children.Where(c => c.Visible))
            {
                if (child.NewLine)
                {
                    builder.OpenElement(9, "div");
                    builder.AddAttribute(10, "class", "runtime-flow-break");
                    builder.CloseElement();
                }

                builder.OpenElement(11, "div");
                builder.AddAttribute(12, "data-widget-id", child.Id);
                builder.AddAttribute(13, "style", styleGetter(child, mode));
                builder.AddContent(14, renderChild(child, mode));
                builder.CloseElement();
            }

            builder.CloseElement(); // body
        }

        builder.CloseElement(); // container
    }
}

