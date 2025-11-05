using System.Collections.Generic;
using BobCrm.App.Models.Widgets;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BobCrm.App.Services.Widgets.Rendering;

/// <summary>
/// 渲染设计态下容器类控件的可视化结构（含嵌套与 Tab）。
/// </summary>
public interface IDesignContainerRenderer
{
    RenderFragment Render(DesignContainerRenderRequest request);
}

/// <summary>
/// 渲染所需的上下文信息，包含事件处理与样式回调。
/// </summary>
public sealed record DesignContainerRenderRequest
{
    public required ContainerWidget Container { get; init; }
    public DraggableWidget? SelectedWidget { get; init; }
    public required ComponentBase EventTarget { get; init; }
    public required Func<DraggableWidget, RenderFragment> RenderChild { get; init; }
    public required Func<DraggableWidget, string> GetWidgetTextStyle { get; init; }
    public required Func<DraggableWidget, string> GetWidgetBackground { get; init; }
    public required Func<DraggableWidget, string> GetNestedChildStyle { get; init; }
    public required Func<DraggableWidget, bool> IsSelected { get; init; }
    public required Action<DraggableWidget> SelectWidget { get; init; }
    public required Func<DragEventArgs, DraggableWidget, Task> OnWidgetDragStart { get; init; }
    public required Func<DragEventArgs, DraggableWidget, Task> OnDropToContainer { get; init; }
    public required Func<DragEventArgs, Task> OnDragEnd { get; init; }
    public Func<DraggableWidget, RenderFragment?>? ChildOverlay { get; init; }

    // Tab container specific
    public IReadOnlyList<TabWidget>? Tabs { get; init; }
    public TabWidget? ActiveTab { get; init; }
    public Action<TabContainerWidget, TabWidget>? ActivateTab { get; init; }
    public Action<TabContainerWidget>? AddTab { get; init; }
    public Action<TabContainerWidget, TabWidget>? RemoveTab { get; init; }
}

/// <summary>
/// 默认容器渲染实现。
/// </summary>
public sealed class DesignContainerRenderer : IDesignContainerRenderer
{
    private readonly EventCallbackFactory _callbackFactory = new();

    public RenderFragment Render(DesignContainerRenderRequest request) => builder =>
    {
        switch (request.Container)
        {
            case TabContainerWidget tabContainer:
                RenderTabContainer(builder, request with { Tabs = request.Tabs ?? Array.Empty<TabWidget>() }, tabContainer);
                break;
            default:
                RenderGenericContainer(builder, request);
                break;
        }
    };

    private void RenderGenericContainer(RenderTreeBuilder builder, DesignContainerRenderRequest request)
    {
        var container = request.Container;
        var children = container.Children ?? new List<DraggableWidget>();
        var hasChildren = children.Count > 0;

        builder.OpenElement(0, "div");
        var baseStyle =
            $"border:1px dashed #52c41a; border-radius:4px; background:{request.GetWidgetBackground(container)}; padding:8px; pointer-events:auto;";
        if (!hasChildren)
        {
            baseStyle += " min-height:80px;";
        }
        builder.AddAttribute(1, "style", baseStyle);

        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "style",
            $"{request.GetWidgetTextStyle(container)} margin-bottom:4px; font-weight:600; font-size:11px; color:#52c41a; pointer-events:none;");
        builder.AddContent(4, $"{container.Label} (嵌套)");
        builder.CloseElement();

        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "class", "frame-drop-zone");
        builder.AddAttribute(7, "data-container-id", container.Id);
        builder.AddAttribute(8, "style",
            $"display:flex; flex-wrap:wrap; align-content:flex-start; align-items:flex-start; gap:4px; {(hasChildren ? "" : "min-height:48px;")}");
        builder.AddAttribute(9, "ondrop", _callbackFactory.Create<DragEventArgs>(
            request.EventTarget, e => request.OnDropToContainer(e, container)));
        builder.AddEventPreventDefaultAttribute(10, "ondrop", true);
        builder.AddEventStopPropagationAttribute(11, "ondrop", true);
        builder.AddEventPreventDefaultAttribute(12, "ondragover", true);

        if (hasChildren)
        {
            foreach (var child in children)
            {
                if (child.NewLine)
                {
                    builder.AddContent(13, FlowBreakFragment);
                }

                builder.AddContent(14, BuildContainerChildFragment(request, child));
            }
        }
        else
        {
            builder.AddContent(15, EmptyContainerPlaceholder);
        }

        builder.CloseElement(); // frame-drop-zone
        builder.CloseElement(); // wrapper
    }

    private void RenderTabContainer(RenderTreeBuilder builder, DesignContainerRenderRequest request, TabContainerWidget container)
    {
        var tabs = request.Tabs ?? Array.Empty<TabWidget>();
        var activeTab = request.ActiveTab;

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style",
            "border:1px solid #d9d9d9; border-radius:6px; background:#fff; padding:8px; width:100%; pointer-events:auto;");

        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "style", "display:flex; gap:4px; border-bottom:1px solid #f0f0f0; padding-bottom:8px; margin-bottom:8px;");

        if (tabs.Count > 0)
        {
            foreach (var tab in tabs)
            {
                var isActive = activeTab != null && activeTab.TabId == tab.TabId;
                builder.AddContent(4, BuildTabHeaderFragment(request, container, tab, isActive));
            }
        }
        else
        {
            builder.AddContent(5, TabListEmptyPlaceholder);
        }

        builder.CloseElement(); // tabs header

        if (activeTab != null)
        {
            builder.OpenElement(20, "div");
            builder.AddAttribute(21, "class", "frame-drop-zone");
            builder.AddAttribute(22, "data-container-id", activeTab.TabId);
            builder.AddAttribute(23, "style",
                "display:flex; flex-wrap:wrap; align-content:flex-start; align-items:flex-start; gap:4px; min-height:64px; background:#fafafa; border-radius:4px; padding:8px;");
            builder.AddAttribute(24, "ondrop",
                _callbackFactory.Create<DragEventArgs>(request.EventTarget, e => request.OnDropToContainer(e, activeTab)));
            builder.AddEventPreventDefaultAttribute(25, "ondrop", true);
            builder.AddEventStopPropagationAttribute(26, "ondrop", true);
            builder.AddEventPreventDefaultAttribute(27, "ondragover", true);

            var children = activeTab.Children ?? new List<DraggableWidget>();
            if (children.Count > 0)
            {
                foreach (var child in children)
                {
                    if (child.NewLine)
                    {
                        builder.AddContent(60, FlowBreakFragment);
                    }

                    builder.AddContent(61, BuildContainerChildFragment(request, child));
                }
            }
            else
            {
                builder.AddContent(62, TabBodyEmptyPlaceholder);
            }

            builder.CloseElement(); // drop zone
        }
        else
        {
            builder.AddContent(50, TabNotCreatedPlaceholder);
        }

        builder.CloseElement(); // wrapper
    }

    private static readonly RenderFragment FlowBreakFragment = builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "flow-break");
        builder.AddAttribute(2, "style", "flex-basis:100%; width:100%; height:0; padding:0; margin:0;");
        builder.CloseElement();
    };

    private static readonly RenderFragment EmptyContainerPlaceholder = builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", "text-align:center; color:#999; font-size:10px; padding:10px; width:100%; pointer-events:none;");
        builder.AddContent(2, "可拖放");
        builder.CloseElement();
    };

    private static readonly RenderFragment TabListEmptyPlaceholder = builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", "font-size:12px; color:#999;");
        builder.AddContent(2, "暂无标签");
        builder.CloseElement();
    };

    private static readonly RenderFragment TabBodyEmptyPlaceholder = builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", "width:100%; text-align:center; color:#999; font-size:11px; pointer-events:none;");
        builder.AddContent(2, "拖拽控件到此");
        builder.CloseElement();
    };

    private static readonly RenderFragment TabNotCreatedPlaceholder = builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style",
            "width:100%; text-align:center; color:#bbb; font-size:12px; padding:16px; border:1px dashed #d9d9d9; border-radius:4px;");
        builder.AddContent(2, "尚未创建标签页");
        builder.CloseElement();
    };

    private RenderFragment BuildContainerChildFragment(DesignContainerRenderRequest request, DraggableWidget child) => builder =>
    {
        var selectedClass = request.IsSelected(child) ? " selected" : string.Empty;
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", $"container-child-widget{selectedClass}");
        builder.AddAttribute(2, "draggable", true);
        builder.AddAttribute(3, "data-drag-type", "widget");
        builder.AddAttribute(4, "data-drag-data", child.Id);
        builder.AddAttribute(5, "style", request.GetNestedChildStyle(child));
        builder.AddAttribute(6, "ondragstart",
            _callbackFactory.Create<DragEventArgs>(request.EventTarget, e => request.OnWidgetDragStart(e, child)));
        builder.AddEventStopPropagationAttribute(7, "ondragstart", true);
        builder.AddAttribute(8, "ondragend",
            _callbackFactory.Create<DragEventArgs>(request.EventTarget, request.OnDragEnd));
        builder.AddAttribute(9, "onclick",
            _callbackFactory.Create<MouseEventArgs>(request.EventTarget, () => request.SelectWidget(child)));
        builder.AddEventStopPropagationAttribute(10, "onclick", true);

        var overlay = request.ChildOverlay?.Invoke(child);
        if (overlay is not null)
        {
            builder.AddContent(11, overlay);
        }

        builder.OpenElement(12, "div");
        builder.AddAttribute(13, "class", "widget-content");
        builder.AddAttribute(14, "style", "width:100%;");
        builder.AddContent(15, request.RenderChild(child));
        builder.CloseElement();

        builder.CloseElement();
    };

    private RenderFragment BuildTabHeaderFragment(
        DesignContainerRenderRequest request,
        TabContainerWidget container,
        TabWidget tab,
        bool isActive) => builder =>
    {
        var tabStyle = isActive
            ? "padding:4px 12px; background:#1890ff; color:#fff; border-radius:4px; font-size:12px; cursor:pointer;"
            : "padding:4px 12px; background:#f5f5f5; color:#555; border-radius:4px; font-size:12px; cursor:pointer;";

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", tabStyle);
        if (request.ActivateTab is not null)
        {
            builder.AddAttribute(2, "onclick",
                _callbackFactory.Create<MouseEventArgs>(request.EventTarget, () =>
                    request.ActivateTab!(container, tab)));
            builder.AddEventStopPropagationAttribute(3, "onclick", true);
        }
        builder.AddContent(4, tab.Label);
        builder.CloseElement();
    };
}
