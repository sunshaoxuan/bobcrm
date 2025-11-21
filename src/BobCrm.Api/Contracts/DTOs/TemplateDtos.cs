using System;
using System.Collections.Generic;
using BobCrm.Api.Base;

namespace BobCrm.Api.Contracts.DTOs;

public record CreateTemplateRequest(
    string Name,
    string? EntityType,
    bool IsUserDefault,
    string? LayoutJson,
    string? Description);

public record UpdateTemplateRequest(
    string? Name,
    string? EntityType,
    bool? IsUserDefault,
    string? LayoutJson,
    string? Description);

public record CopyTemplateRequest(
    string? Name,
    string? EntityType,
    FormTemplateUsageType? UsageType,
    string? Description);

public record TemplateBindingDto(
    int Id,
    string EntityType,
    FormTemplateUsageType UsageType,
    int TemplateId,
    bool IsSystem,
    string? RequiredFunctionCode,
    string? UpdatedBy,
    DateTime UpdatedAt);

public record TemplateDescriptorDto(
    int Id,
    string Name,
    string? EntityType,
    FormTemplateUsageType UsageType,
    string? LayoutJson,
    List<string> Tags,
    string? Description);

public record UpsertTemplateBindingRequest(
    string EntityType,
    FormTemplateUsageType UsageType,
    int TemplateId,
    bool IsSystem,
    string? RequiredFunctionCode);

public record TemplateRuntimeRequest(
    FormTemplateUsageType UsageType = FormTemplateUsageType.Detail,
    string? FunctionCodeOverride = null);

public record TemplateRuntimeResponse(
    TemplateBindingDto Binding,
    TemplateDescriptorDto Template,
    bool HasFullAccess,
    IReadOnlyList<string> AppliedScopes);
