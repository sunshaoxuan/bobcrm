using System.Text.Json.Serialization;
using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.Api.Contracts.DTOs.Enum;

/// <summary>
/// 枚举选项 DTO
/// </summary>
public class EnumOptionDto
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;
    /// <summary>
    /// 单语显示名（单语模式返回）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }
    /// <summary>
    /// 单语描述（单语模式返回）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
    /// <summary>
    /// 多语显示名（向后兼容）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MultilingualText? DisplayNameTranslations { get; set; }
    /// <summary>
    /// 多语描述（向后兼容）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MultilingualText? DescriptionTranslations { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; }
    public string? ColorTag { get; set; }
    public string? Icon { get; set; }
}
