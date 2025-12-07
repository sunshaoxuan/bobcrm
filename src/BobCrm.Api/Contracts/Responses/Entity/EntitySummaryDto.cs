using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.Api.Contracts.Responses.Entity;

/// <summary>
/// 实体摘要 DTO - 用于列表展示
/// </summary>
public class EntitySummaryDto
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityRoute { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public MultilingualText? DisplayName { get; set; }
    public MultilingualText? Description { get; set; }
    public string? ApiEndpoint { get; set; }
    public string? Icon { get; set; }
    public string? Category { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsRootEntity { get; set; }
    public string Status { get; set; } = string.Empty;
}
