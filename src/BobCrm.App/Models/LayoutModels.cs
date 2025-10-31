using System.Text.Json.Serialization;

namespace BobCrm.App.Models;

/// <summary>
/// 自由布局模型 - 支持绝对定位
/// </summary>
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

/// <summary>
/// 自由布局控件 - 绝对定位属性
/// </summary>
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

/// <summary>
/// Flow 布局模型 - 兼容旧版
/// </summary>
public class FlowLayout
{
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "flow";

    [JsonPropertyName("items")]
    public Dictionary<string, FlowItem> Items { get; set; } = new();
}

public class FlowItem
{
    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("w")]
    public int W { get; set; } = 6; // 栅格宽度（1-12）

    [JsonPropertyName("visible")]
    public bool Visible { get; set; } = true;
}

/// <summary>
/// 拖拽状态（前端使用）
/// </summary>
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

/// <summary>
/// 缩放手柄方向
/// </summary>
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
