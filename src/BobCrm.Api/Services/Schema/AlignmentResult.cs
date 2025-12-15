namespace BobCrm.Api.Services;

/// <summary>
/// 对齐结果枚举
/// </summary>
public enum AlignmentResult
{
    /// <summary>已对齐（执行了修改）</summary>
    Aligned,
    /// <summary>已经对齐（无需修改）</summary>
    AlreadyAligned,
    /// <summary>对齐失败</summary>
    Failed
}

