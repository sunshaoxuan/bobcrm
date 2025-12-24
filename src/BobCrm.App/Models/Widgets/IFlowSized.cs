namespace BobCrm.App.Models.Widgets;

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
