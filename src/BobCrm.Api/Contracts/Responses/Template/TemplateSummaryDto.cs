using BobCrm.Api.Base;

namespace BobCrm.Api.Contracts.Responses.Template;

/// <summary>
/// 模板摘要 DTO。
/// </summary>
public class TemplateSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public FormTemplateUsageType UsageType { get; set; }
    public bool IsUserDefault { get; set; }
    public bool IsSystemDefault { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsInUse { get; set; }
}
