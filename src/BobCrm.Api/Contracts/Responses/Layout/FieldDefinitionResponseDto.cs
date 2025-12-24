namespace BobCrm.Api.Contracts.Responses.Layout;

/// <summary>
/// 字段定义响应 DTO。
/// </summary>
public class FieldDefinitionResponseDto
{
    /// <summary>
    /// 字段键。
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 字段显示名称（已按语言解析）。
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 字段类型。
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 是否必填。
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// 验证规则（JSON 字符串，可选）。
    /// </summary>
    public string? Validation { get; set; }

    /// <summary>
    /// 默认值（可选）。
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 标签列表。
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 字段动作列表。
    /// </summary>
    public List<FieldActionResponseDto> Actions { get; set; } = new();
}

