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
