using System.Text.Json.Serialization;

namespace BobCrm.App.Models;

/// <summary>
/// 【已废弃】Flow 布局模型 - 兼容旧版
/// 废弃原因：已被 DraggableWidget + LayoutOptions 替代
/// 建议使用：DraggableWidget.Layout (LayoutMode.Flow)
/// </summary>
[Obsolete("Use DraggableWidget with LayoutOptions (LayoutMode.Flow) instead", false)]
public class FlowLayout
{
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "flow";

    [JsonPropertyName("items")]
    public Dictionary<string, FlowItem> Items { get; set; } = new();
}
