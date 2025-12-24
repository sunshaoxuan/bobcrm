namespace BobCrm.App.Models;

public record TemplateDescriptorDto(
    int Id,
    string Name,
    string? EntityType,
    TemplateUsageType UsageType,
    string? LayoutJson,
    IReadOnlyList<string> Tags,
    string? Description);
