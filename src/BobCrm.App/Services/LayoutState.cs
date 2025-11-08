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

    private void Notify() => OnChanged?.Invoke();
}
