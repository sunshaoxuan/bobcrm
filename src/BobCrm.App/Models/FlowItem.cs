using System.Text.Json.Serialization;

namespace BobCrm.App.Models;

/// <summary>
/// 【已废弃】Flow 布局项
/// 废弃原因：已被 DraggableWidget (实现 IFlowSized) 替代
/// </summary>
[Obsolete("Use DraggableWidget and IFlowSized interface instead", false)]
public class FlowItem
{
    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("w")]
    public int W { get; set; } = 6; // 栅格宽度（1-12）

    [JsonPropertyName("visible")]
    public bool Visible { get; set; } = true;
}
