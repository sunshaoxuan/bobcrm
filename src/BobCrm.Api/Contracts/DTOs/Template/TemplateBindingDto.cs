using BobCrm.Api.Base;

namespace BobCrm.Api.Contracts.DTOs.Template;

/// <summary>
/// 模板绑定 DTO
/// </summary>
public record TemplateBindingDto(
    int Id,
    string EntityType,
    FormTemplateUsageType UsageType,
    int TemplateId,
    bool IsSystem,
    string? RequiredFunctionCode,
    string? UpdatedBy,
    DateTime UpdatedAt);
