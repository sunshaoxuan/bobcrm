namespace BobCrm.App.Models.Widgets;

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
