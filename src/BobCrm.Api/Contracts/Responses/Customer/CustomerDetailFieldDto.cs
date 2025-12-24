namespace BobCrm.Api.Contracts.Responses.Customer;

/// <summary>
/// 客户字段展示 DTO。
/// </summary>
public class CustomerDetailFieldDto
{
    /// <summary>
    /// 字段键。
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 字段展示名称（已按语言解析）。
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 字段类型。
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 字段值（字符串形式，原逻辑会将 JSON 字符串去引号）。
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// 是否必填。
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// 字段验证规则（JSON 字符串，可选）。
    /// </summary>
    public string? Validation { get; set; }
}

