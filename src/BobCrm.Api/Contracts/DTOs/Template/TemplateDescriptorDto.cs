using BobCrm.Api.Base;

namespace BobCrm.Api.Contracts.DTOs.Template;

/// <summary>
/// 模板描述 DTO
/// </summary>
public record TemplateDescriptorDto(
    int Id,
    string Name,
    string? EntityType,
    FormTemplateUsageType UsageType,
    string? LayoutJson,
    List<string> Tags,
    string? Description);
