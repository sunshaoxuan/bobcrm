using System;

namespace BobCrm.App.Services;

/// <summary>
/// 管理应用外壳（侧栏/右栏/移动遮罩）的状态，便于在任何组件中统一控制。
/// </summary>
public class LayoutState
{
    public bool IsSiderCollapsed { get; private set; }
    public bool IsSiderOverlayOpen { get; private set; }
    public bool IsRightPanelVisible { get; private set; }
    public NavDisplayMode NavMode { get; private set; } = NavDisplayMode.IconText;
    public string? CurrentDomainCode { get; private set; }

    // 新增：菜单面板状态
    public bool IsMenuPanelOpen { get; private set; }
    public bool IsDomainSelectorOpen { get; private set; }

    public event Action? OnChanged;

    public void ToggleSiderCollapsed() => SetSiderCollapsed(!IsSiderCollapsed);

    public void SetSiderCollapsed(bool collapsed)
    {
        if (collapsed == IsSiderCollapsed) return;
        IsSiderCollapsed = collapsed;
        Notify();
    }

    public void ToggleSiderOverlay() => SetSiderOverlay(!IsSiderOverlayOpen);

    public void SetSiderOverlay(bool open)
    {
        if (open == IsSiderOverlayOpen) return;
        IsSiderOverlayOpen = open;
        Notify();
    }

    public void ToggleRightPanel() => SetRightPanelVisible(!IsRightPanelVisible);

    public void SetRightPanelVisible(bool visible)
    {
        if (visible == IsRightPanelVisible) return;
        IsRightPanelVisible = visible;
        Notify();
    }

    public void SetNavMode(NavDisplayMode mode)
    {
        if (NavMode == mode) return;
        NavMode = mode;
        Notify();
    }

    public void SetCurrentDomain(string? code)
    {
        if (string.Equals(CurrentDomainCode, code, StringComparison.OrdinalIgnoreCase)) return;
        CurrentDomainCode = code;
        Notify();
    }

    public void ToggleMenuPanel() => SetMenuPanelOpen(!IsMenuPanelOpen);

    public void OpenMenuPanel()
    {
        SetMenuPanelOpen(true);
    }

    public void CloseMenuPanel()
    {
        SetMenuPanelOpen(false);
    }

    private void SetMenuPanelOpen(bool open)
    {
        if (open == IsMenuPanelOpen) return;
        IsMenuPanelOpen = open;
        // 关闭菜单面板时，也关闭领域选择器
        if (!open)
        {
            IsDomainSelectorOpen = false;
        }
        Notify();
    }

    public void ToggleDomainSelector() => SetDomainSelectorOpen(!IsDomainSelectorOpen);

    public void CloseDomainSelector()
    {
        SetDomainSelectorOpen(false);
    }

    private void SetDomainSelectorOpen(bool open)
    {
        if (open == IsDomainSelectorOpen) return;
        IsDomainSelectorOpen = open;
        // 打开领域选择器时，关闭菜单面板
        if (open)
        {
            IsMenuPanelOpen = false;
        }
        Notify();
    }

    public enum NavDisplayMode
    {
        Icons,
        Labels,
        IconText
    }

    private void Notify() => OnChanged?.Invoke();
}
