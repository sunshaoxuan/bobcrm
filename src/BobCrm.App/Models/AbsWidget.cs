using System.Text.Json.Serialization;

namespace BobCrm.App.Models;

/// <summary>
/// 【已废弃】自由布局控件 - 绝对定位属性
/// 废弃原因：已被 DraggableWidget (实现 IAbsolutePositioned) 替代
/// 建议使用：DraggableWidget 及其子类
/// </summary>
[Obsolete("Use DraggableWidget and its subclasses instead", false)]
public class AbsWidget
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("dataField")]
    public string? DataField { get; set; }

    // 绝对定位属性（像素）
    [JsonPropertyName("x")]
    public int X { get; set; } = 0;

    [JsonPropertyName("y")]
    public int Y { get; set; } = 0;

    [JsonPropertyName("w")]
    public int W { get; set; } = 360;

    [JsonPropertyName("h")]
    public int H { get; set; } = 56;

    // 层叠和状态
    [JsonPropertyName("zIndex")]
    public int ZIndex { get; set; } = 1;

    [JsonPropertyName("locked")]
    public bool Locked { get; set; } = false;

    [JsonPropertyName("visible")]
    public bool Visible { get; set; } = true;

    // 容器关系（用于 frame/tabbox）
    [JsonPropertyName("containerId")]
    public string? ContainerId { get; set; }

    // 附加属性（可选，用于扩展）
    [JsonPropertyName("properties")]
    public Dictionary<string, object>? Properties { get; set; }
}
