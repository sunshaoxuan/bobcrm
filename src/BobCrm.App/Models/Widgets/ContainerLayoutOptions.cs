namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 容器内部的布局选项
/// 定义容器如何布局其子控件（与 LayoutOptions 的"作为项"不同）
/// </summary>
public class ContainerLayoutOptions
{
    /// <summary>容器内布局模式</summary>
    public ContainerLayoutMode Mode { get; set; } = ContainerLayoutMode.Flow;

    /// <summary>Flex方向（row/column）</summary>
    public string FlexDirection { get; set; } = "row";

    /// <summary>是否换行</summary>
    public bool FlexWrap { get; set; } = true;

    /// <summary>主轴对齐方式</summary>
    public string JustifyContent { get; set; } = "flex-start";

    /// <summary>交叉轴对齐方式</summary>
    public string AlignItems { get; set; } = "flex-start";

    /// <summary>间距（像素）</summary>
    public int Gap { get; set; } = 8;

    /// <summary>内边距（像素）</summary>
    public int Padding { get; set; } = 12;

    /// <summary>背景色</summary>
    public string BackgroundColor { get; set; } = "#ffffff";

    /// <summary>圆角（像素）</summary>
    public int BorderRadius { get; set; } = 4;

    /// <summary>边框样式</summary>
    public string BorderStyle { get; set; } = "solid";

    /// <summary>边框颜色</summary>
    public string BorderColor { get; set; } = "#d9d9d9";

    /// <summary>边框宽度（像素）</summary>
    public int BorderWidth { get; set; } = 1;

    /// <summary>
    /// 生成容器内部的 CSS 样式
    /// </summary>
    public string GetContainerCssStyle()
    {
        var styles = new List<string>();

        // 基础布局
        styles.Add("display: flex");
        styles.Add($"flex-direction: {FlexDirection}");
        styles.Add($"flex-wrap: {(FlexWrap ? "wrap" : "nowrap")}");
        styles.Add($"justify-content: {JustifyContent}");
        styles.Add($"align-items: {AlignItems}");
        styles.Add($"gap: {Gap}px");

        // 外观
        styles.Add($"padding: {Padding}px");
        styles.Add($"background-color: {BackgroundColor}");
        styles.Add($"border-radius: {BorderRadius}px");

        if (BorderWidth > 0)
        {
            styles.Add($"border: {BorderWidth}px {BorderStyle} {BorderColor}");
        }

        // 容器必需属性
        styles.Add("position: relative");
        styles.Add("min-height: 60px"); // 确保空容器可见

        return string.Join("; ", styles);
    }
}

/// <summary>
/// 容器内布局模式
/// </summary>
public enum ContainerLayoutMode
{
    /// <summary>流式布局（基于 Flex，自动换行）</summary>
    Flow,

    /// <summary>Flex 布局（不自动换行）</summary>
    Flex,

    /// <summary>绝对定位</summary>
    Absolute
}
