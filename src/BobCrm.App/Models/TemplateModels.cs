using System;
using System.Collections.Generic;
using System.Text.Json;

namespace BobCrm.App.Models;

public enum TemplateUsageType
{
    Detail = 0,
    Edit = 1,
    List = 2,
    Combined = 3
}

public record TemplateBindingDto(
    int Id,
    string EntityType,
    TemplateUsageType UsageType,
    int TemplateId,
    bool IsSystem,
    string? RequiredFunctionCode,
    string UpdatedBy,
    DateTime UpdatedAt);

public record TemplateDescriptorDto(
    int Id,
    string Name,
    string? EntityType,
    TemplateUsageType UsageType,
    string? LayoutJson,
    IReadOnlyList<string> Tags,
    string? Description);

public record TemplateRuntimeResponse(
    TemplateBindingDto Binding,
    TemplateDescriptorDto Template,
    bool HasFullAccess,
    IReadOnlyList<string> AppliedScopes);

public record TemplateRuntimeRequest
{
    public TemplateUsageType UsageType { get; init; } = TemplateUsageType.Detail;
    public string? FunctionCodeOverride { get; init; }
    public int? EntityId { get; init; }
    public JsonElement? EntityData { get; init; }
}
