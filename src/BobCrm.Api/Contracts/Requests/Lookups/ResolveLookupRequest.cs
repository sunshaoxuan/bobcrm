namespace BobCrm.Api.Contracts.Requests.Lookups;

/// <summary>
/// 批量解析 Lookup 显示名请求
/// </summary>
public sealed class ResolveLookupRequest
{
    /// <summary>
    /// 目标实体：可传 EntityRoute（如 "customer"）或 FullTypeName。
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// 目标实体用于展示的字段名（可选，默认自动选择 Name/Title/Code 等）。
    /// </summary>
    public string? DisplayField { get; set; }

    /// <summary>
    /// 待解析的主键值列表（字符串形式）。
    /// </summary>
    public List<string> Ids { get; set; } = new();
}

