namespace BobCrm.App.Models;

public record TemplateBindingDto(
    int Id,
    string EntityType,
    TemplateUsageType UsageType,
    int TemplateId,
    bool IsSystem,
    string? RequiredFunctionCode,
    string UpdatedBy,
    DateTime UpdatedAt);
