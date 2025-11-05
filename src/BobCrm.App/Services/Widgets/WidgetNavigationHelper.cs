using BobCrm.App.Models.Widgets;

namespace BobCrm.App.Services.Widgets;

/// <summary>
/// Widget导航助手
/// 负责在Widget树中查找、遍历Widget
/// </summary>
public static class WidgetNavigationHelper
{
    /// <summary>
    /// 在Widget列表中查找指定ID的Widget（递归）
    /// </summary>
    public static DraggableWidget? FindWidget(List<DraggableWidget> widgets, string widgetId)
    {
        foreach (var w in widgets)
        {
            if (w.Id == widgetId)
                return w;

            if (w is ContainerWidget container && container.Children != null)
            {
                var found = FindWidgetInContainers(container.Children, widgetId);
                if (found != null)
                    return found;
            }
        }
        return null;
    }

    /// <summary>
    /// 在容器列表中递归查找Widget
    /// </summary>
    public static DraggableWidget? FindWidgetInContainers(List<DraggableWidget> widgets, string widgetId)
    {
        foreach (var w in widgets)
        {
            if (w.Id == widgetId)
                return w;

            if (w is ContainerWidget container && container.Children != null)
            {
                var found = FindWidgetInContainers(container.Children, widgetId);
                if (found != null)
                    return found;
            }
        }
        return null;
    }

    /// <summary>
    /// 查找Widget的父容器
    /// </summary>
    public static DraggableWidget? FindParentContainer(List<DraggableWidget> rootWidgets, DraggableWidget targetWidget)
    {
        foreach (var widget in rootWidgets)
        {
            if (widget is ContainerWidget container && container.Children != null)
            {
                var parent = FindParentInContainer(container, targetWidget);
                if (parent != null)
                    return parent;
            }
        }
        return null;
    }

    /// <summary>
    /// 在容器内递归查找Widget的父容器
    /// </summary>
    public static DraggableWidget? FindParentInContainer(DraggableWidget container, DraggableWidget targetWidget)
    {
        if (container is not ContainerWidget cont || cont.Children == null)
            return null;

        // 检查是否是直接子元素
        if (cont.Children.Any(c => ReferenceEquals(c, targetWidget)))
            return container;

        // 递归查找
        foreach (var child in cont.Children.OfType<ContainerWidget>())
        {
            if (child.Children != null)
            {
                var parent = FindParentInContainer(child, targetWidget);
                if (parent != null)
                    return parent;
            }
        }

        return null;
    }

    /// <summary>
    /// 递归遍历所有Widget并执行操作
    /// </summary>
    public static void TraverseWidgets(IEnumerable<DraggableWidget> widgets, Action<DraggableWidget> action)
    {
        foreach (var widget in widgets)
        {
            action(widget);

            if (widget is ContainerWidget container && container.Children != null)
            {
                TraverseWidgets(container.Children, action);
            }
        }
    }
}

