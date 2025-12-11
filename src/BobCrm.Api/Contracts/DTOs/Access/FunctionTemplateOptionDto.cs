using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Contracts.DTOs.Access;

/// <summary>
/// 功能模板选项 DTO
/// </summary>
public record FunctionTemplateOptionDto
{
    public int BindingId { get; init; }
    public int TemplateId { get; init; }
    public string TemplateName { get; init; } = string.Empty;
    public string EntityType { get; init; } = string.Empty;
    public FormTemplateUsageType UsageType { get; init; } = FormTemplateUsageType.Detail;
    public bool IsSystem { get; init; }
    public bool IsDefault { get; init; }
}
