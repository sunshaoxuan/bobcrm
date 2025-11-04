namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 布局选项 - 组合管理不同布局模式的属性
/// 支持Flow、Absolute、Flex三种布局模式
/// </summary>
public class LayoutOptions
{
    /// <summary>当前布局模式</summary>
    public LayoutMode Mode { get; set; } = LayoutMode.Flow;

    // ==== Flow布局属性 ====

    /// <summary>宽度数值</summary>
    public int Width { get; set; } = 48;

    /// <summary>宽度单位（%或px）</summary>
    public string WidthUnit { get; set; } = "%";

    /// <summary>高度数值</summary>
    public int Height { get; set; } = 40;

    /// <summary>高度单位（px或auto）</summary>
    public string HeightUnit { get; set; } = "auto";

    /// <summary>是否从新行开始</summary>
    public bool NewLine { get; set; } = false;

    // ==== Absolute布局属性 ====

    /// <summary>X坐标（像素）</summary>
    public int X { get; set; } = 0;

    /// <summary>Y坐标（像素）</summary>
    public int Y { get; set; } = 0;

    /// <summary>绝对定位宽度（像素）</summary>
    public int W { get; set; } = 200;

    /// <summary>绝对定位高度（像素）</summary>
    public int H { get; set; } = 100;

    /// <summary>Z-Index层级</summary>
    public int ZIndex { get; set; } = 1;

    // ==== Flex布局属性 ====

    /// <summary>Flex grow系数</summary>
    public int FlexGrow { get; set; } = 0;

    /// <summary>Flex shrink系数</summary>
    public int FlexShrink { get; set; } = 1;

    /// <summary>Flex basis基准值</summary>
    public string FlexBasis { get; set; } = "auto";

    /// <summary>对齐方式</summary>
    public string AlignSelf { get; set; } = "auto";

    /// <summary>
    /// 获取当前模式下的CSS样式字符串
    /// </summary>
    public string GetCssStyle()
    {
        return Mode switch
        {
            LayoutMode.Absolute => GetAbsoluteStyle(),
            LayoutMode.Flex => GetFlexStyle(),
            LayoutMode.Flow => GetFlowStyle(),
            _ => GetFlowStyle()
        };
    }

    private string GetFlowStyle()
    {
        var width = WidthUnit == "%"
            ? $"calc({Width}% - 8px)"
            : $"{Width}px";

        // 高度策略：只有显式 px 时才输出 height；auto 时不设高度，让内容决定
        var heightStyle = HeightUnit == "px"
            ? $"min-height:{Height}px;"
            : "";  // auto 时不输出任何高度

        return $"flex:0 0 {width}; {heightStyle}".TrimEnd();
    }

    private string GetAbsoluteStyle()
    {
        return $"position:absolute; left:{X}px; top:{Y}px; width:{W}px; height:{H}px; z-index:{ZIndex};";
    }

    private string GetFlexStyle()
    {
        return $"flex:{FlexGrow} {FlexShrink} {FlexBasis}; align-self:{AlignSelf};";
    }
}
