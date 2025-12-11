using System.Text.Json.Serialization;
using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.Api.Contracts.DTOs.Enum;

/// <summary>
/// 枚举定义 DTO
/// </summary>
public class EnumDefinitionDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
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
    public bool IsSystem { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<EnumOptionDto> Options { get; set; } = new();
}
