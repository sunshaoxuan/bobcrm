namespace BobCrm.Api.Contracts.Responses.Layout;

/// <summary>
/// 字段标签统计 DTO。
/// </summary>
public class FieldTagSummaryDto
{
    /// <summary>
    /// 标签名。
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// 计数。
    /// </summary>
    public int Count { get; set; }
}

