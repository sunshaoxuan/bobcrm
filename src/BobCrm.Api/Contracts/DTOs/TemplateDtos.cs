using System;
using System.Collections.Generic;
using BobCrm.Api.Domain;

namespace BobCrm.Api.Contracts.DTOs;

public record TemplateBindingDto(
    int Id,
    string EntityType,
    FormTemplateUsageType UsageType,
    int TemplateId,
    bool IsSystem,
    string? RequiredFunctionCode,
    string UpdatedBy,
    DateTime UpdatedAt);

public record TemplateDescriptorDto(
    int Id,
    string Name,
    string? EntityType,
    FormTemplateUsageType UsageType,
    string? LayoutJson,
    IReadOnlyList<string> Tags,
    string? Description);

public record TemplateRuntimeRequest
{
    public FormTemplateUsageType UsageType { get; init; } = FormTemplateUsageType.Detail;
    public string? FunctionCodeOverride { get; init; }
}

public record TemplateRuntimeResponse(
    TemplateBindingDto Binding,
    TemplateDescriptorDto Template,
    bool HasFullAccess,
    IReadOnlyList<string> AppliedScopes);

public record UpsertTemplateBindingRequest
{
    public string EntityType { get; init; } = string.Empty;
    public FormTemplateUsageType UsageType { get; init; } = FormTemplateUsageType.Detail;
    public int TemplateId { get; init; }
    public bool IsSystem { get; init; } = true;
    public string? RequiredFunctionCode { get; init; }
}
