using System.Text.Json.Serialization;

namespace BobCrm.App.Models;

/// <summary>
/// 【已废弃】自由布局模型 - 支持绝对定位
/// 废弃原因：已被 DraggableWidget + LayoutOptions 替代
/// 建议使用：DraggableWidget.Layout (LayoutMode.Absolute)
/// 保留此文件仅供参考，未来版本将删除
/// </summary>
[Obsolete("Use DraggableWidget with LayoutOptions (LayoutMode.Absolute) instead", false)]
public class AbsLayout
{
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "free";

    [JsonPropertyName("widgets")]
    public List<AbsWidget> Widgets { get; set; } = new();

    [JsonPropertyName("grid")]
    public int Grid { get; set; } = 8; // 网格大小（px）

    [JsonPropertyName("canvasWidth")]
    public int CanvasWidth { get; set; } = 1200;

    [JsonPropertyName("canvasHeight")]
    public int CanvasHeight { get; set; } = 800;
}
