namespace BobCrm.App.Models;

/// <summary>
/// 【已废弃】拖拽状态（前端使用）
/// 废弃原因：已被统一的 DragManager.js 替代
/// </summary>
[Obsolete("Use DragManager.js for drag state management instead", false)]
public class DragState
{
    public string? DraggedId { get; set; }
    public string? DragType { get; set; } // "component" | "widget"
    public int StartX { get; set; }
    public int StartY { get; set; }
    public int OffsetX { get; set; }
    public int OffsetY { get; set; }
    public bool IsDragging { get; set; }
}
