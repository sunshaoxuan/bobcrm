namespace BobCrm.App.Models.Widgets;

/// <summary>
/// 绝对定位能力接口
/// 用于支持自由布局模式（Free Layout）
/// </summary>
public interface IAbsolutePositioned
{
    /// <summary>X坐标（像素）</summary>
    int X { get; set; }

    /// <summary>Y坐标（像素）</summary>
    int Y { get; set; }

    /// <summary>宽度（像素）</summary>
    int W { get; set; }

    /// <summary>高度（像素）</summary>
    int H { get; set; }

    /// <summary>Z-Index层级</summary>
    int ZIndex { get; set; }
}

/// <summary>
/// 流式布局能力接口
/// 用于支持自动换行的流式布局模式（Flow Layout）
/// </summary>
public interface IFlowSized
{
    /// <summary>宽度数值</summary>
    int Width { get; set; }

    /// <summary>宽度单位（%或px）</summary>
    string WidthUnit { get; set; }

    /// <summary>高度数值</summary>
    int Height { get; set; }

    /// <summary>高度单位（px或auto）</summary>
    string HeightUnit { get; set; }

    /// <summary>是否从新行开始</summary>
    bool NewLine { get; set; }
}

/// <summary>
/// Flex布局能力接口
/// 用于支持Flexbox布局模式
/// </summary>
public interface IFlexItem
{
    /// <summary>Flex grow系数</summary>
    int FlexGrow { get; set; }

    /// <summary>Flex shrink系数</summary>
    int FlexShrink { get; set; }

    /// <summary>Flex basis基准值</summary>
    string FlexBasis { get; set; }

    /// <summary>对齐方式（flex-start, center, flex-end, stretch）</summary>
    string AlignSelf { get; set; }
}

/// <summary>
/// 布局模式枚举
/// </summary>
public enum LayoutMode
{
    /// <summary>流式布局（默认）</summary>
    Flow,

    /// <summary>绝对定位布局</summary>
    Absolute,

    /// <summary>Flexbox布局</summary>
    Flex
}
