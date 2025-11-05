using BobCrm.App.Models.Widgets;

namespace BobCrm.App.Services.Widgets;

/// <summary>
/// Tab状态管理器
/// 负责管理TabContainer的激活状态
/// </summary>
public class TabStateManager
{
    private readonly Dictionary<string, string> _activeTabs = new();

    /// <summary>
    /// 获取激活的Tab
    /// </summary>
    public TabWidget? GetActiveTab(TabContainerWidget container)
    {
        if (container.Children == null || container.Children.Count == 0)
            return null;

        var tabs = container.Children.OfType<TabWidget>().ToList();
        if (tabs.Count == 0)
            return null;

        // 尝试从状态字典获取
        if (_activeTabs.TryGetValue(container.Id, out var tabId))
        {
            var found = tabs.FirstOrDefault(t => t.TabId == tabId);
            if (found != null)
                return found;
        }

        // 查找默认Tab
        var defaultTab = tabs.FirstOrDefault(t => t.IsDefault);
        if (defaultTab != null)
        {
            _activeTabs[container.Id] = defaultTab.TabId;
            return defaultTab;
        }

        // 返回第一个Tab
        var firstTab = tabs.First();
        _activeTabs[container.Id] = firstTab.TabId;
        return firstTab;
    }

    /// <summary>
    /// 激活指定的Tab
    /// </summary>
    public void ActivateTab(TabContainerWidget container, TabWidget tab)
    {
        if (container.Children?.OfType<TabWidget>().Any(t => t.TabId == tab.TabId) == true)
        {
            _activeTabs[container.Id] = tab.TabId;
            container.ActiveTabId = tab.TabId;
            
            // 更新所有Tab的IsDefault状态
            foreach (var t in container.Children.OfType<TabWidget>())
            {
                t.IsDefault = t.TabId == tab.TabId;
            }
        }
    }

    /// <summary>
    /// 添加新Tab时自动激活
    /// </summary>
    public void RegisterNewTab(TabContainerWidget container, TabWidget newTab)
    {
        _activeTabs[container.Id] = newTab.TabId;
    }

    /// <summary>
    /// 清空所有Tab状态
    /// </summary>
    public void Clear() => _activeTabs.Clear();

    /// <summary>
    /// 添加新的Tab到容器
    /// </summary>
    public TabWidget AddTab(TabContainerWidget container)
    {
        container.Children ??= new List<DraggableWidget>();
        var nextIndex = container.Children.OfType<TabWidget>().Count() + 1;
        var newTab = new TabWidget { Label = $"Tab {nextIndex}" };
        container.Children.Add(newTab);
        RegisterNewTab(container, newTab);
        return newTab;
    }

    /// <summary>
    /// 从容器中移除Tab（确保至少保留一个Tab）
    /// </summary>
    /// <returns>是否成功移除</returns>
    public bool RemoveTab(TabContainerWidget container, TabWidget tab)
    {
        var tabs = container.Children?.OfType<TabWidget>().ToList() ?? new List<TabWidget>();
        if (tabs.Count <= 1)
            return false; // 至少保留一个Tab

        if (container.Children != null)
        {
            container.Children.Remove(tab);
        }

        // 如果删除的是激活的Tab，切换到第一个Tab
        var currentActive = GetActiveTab(container);
        if (currentActive?.TabId == tab.TabId)
        {
            var next = container.Children?.OfType<TabWidget>().FirstOrDefault();
            if (next != null)
            {
                _activeTabs[container.Id] = next.TabId;
                next.IsDefault = true;
                container.ActiveTabId = next.TabId;
            }
            else
            {
                container.ActiveTabId = null;
            }
        }

        return true;
    }
}

