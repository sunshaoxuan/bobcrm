using System.Text.Json.Serialization;
using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.Api.Contracts.Responses.Entity;

/// <summary>
/// 实体摘要 DTO - 用于列表展示
/// </summary>
public class EntitySummaryDto
{
    /// <summary>
    /// 实体类型（通常为路由/FullTypeName）
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
    /// <summary>
    /// 实体路由（用于URL）
    /// </summary>
    public string EntityRoute { get; set; } = string.Empty;
    /// <summary>
    /// 实体名称（代码层名称）
    /// </summary>
    public string EntityName { get; set; } = string.Empty;
    /// <summary>
    /// 单语显示名（单语模式下返回）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }
    /// <summary>
    /// 单语描述（单语模式下返回）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
    /// <summary>
    /// 多语显示名（向后兼容模式）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MultilingualText? DisplayNameTranslations { get; set; }
    /// <summary>
    /// 多语描述（向后兼容模式）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MultilingualText? DescriptionTranslations { get; set; }
    public string? ApiEndpoint { get; set; }
    public string? Icon { get; set; }
    public string? Category { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsRootEntity { get; set; }
    public string Status { get; set; } = string.Empty;
}
