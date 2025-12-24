namespace BobCrm.App.Models;

/// <summary>
/// 【已废弃】缩放手柄方向
/// 废弃原因：已被 DragManager.js 的 direction 参数替代
/// </summary>
[Obsolete("Use DragManager.js direction parameter instead", false)]
public enum ResizeHandle
{
    None,
    TopLeft,
    Top,
    TopRight,
    Right,
    BottomRight,
    Bottom,
    BottomLeft,
    Left
}
