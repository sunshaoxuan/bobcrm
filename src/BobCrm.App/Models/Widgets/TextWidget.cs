namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 文本控件抽象基类 - 所有显示文本的控件的基类
/// 提供字体、颜色、对齐等文本样式属性
/// </summary>
public abstract class TextWidget : DraggableWidget
{
    // ===== 文本样式属性 =====

    /// <summary>字体大小（像素）</summary>
    public int FontSize { get; set; } = 14;

    /// <summary>字体颜色（十六进制颜色值）</summary>
    public string FontColor { get; set; } = "#333333";

    /// <summary>字体族</summary>
    public string FontFamily { get; set; } = "inherit";

    /// <summary>字体粗细（normal, bold）</summary>
    public string FontWeight { get; set; } = "normal";

    /// <summary>文本对齐方式（left, center, right）</summary>
    public string TextAlign { get; set; } = "left";
}
