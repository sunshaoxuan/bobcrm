using System.Collections.Generic;
using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Contracts.DTOs;

public class MultilingualText : Dictionary<string, string?>
{
    public MultilingualText() : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    public MultilingualText(IDictionary<string, string?> source) : base(source, StringComparer.OrdinalIgnoreCase)
    {
    }
}

public record FunctionNodeDto
{
    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public MultilingualText? DisplayNameTranslations { get; init; }
    public string? Route { get; init; }
    public string? Icon { get; init; }
    public bool IsMenu { get; init; }
    public int SortOrder { get; init; }
    public int? TemplateId { get; init; }
    public string? TemplateName { get; init; }
    public List<FunctionNodeDto> Children { get; init; } = new();
    public List<FunctionTemplateOptionDto> TemplateOptions { get; init; } = new();
    public List<FunctionNodeTemplateBindingDto> TemplateBindings { get; init; } = new();
}

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

public record FunctionNodeTemplateBindingDto(
    int BindingId,
    string EntityType,
    FormTemplateUsageType UsageType,
    int TemplateId,
    string TemplateName,
    bool IsSystem);

public record CreateFunctionRequest
{
    public Guid? ParentId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public MultilingualText? DisplayName { get; init; }
    public string? Route { get; init; }
    public string? Icon { get; init; }
    public bool IsMenu { get; init; } = true;
    public int SortOrder { get; init; } = 100;
    public int? TemplateId { get; init; }
}

public record UpdateFunctionRequest
{
    public Guid? ParentId { get; init; }
    public bool ClearParent { get; init; }
    public string? Name { get; init; }
    public MultilingualText? DisplayName { get; init; }
    public string? Route { get; init; }
    public bool ClearRoute { get; init; }
    public string? Icon { get; init; }
    public bool? IsMenu { get; init; }
    public int? SortOrder { get; init; }
    public int? TemplateId { get; init; }
    public bool ClearTemplate { get; init; }
}

public record FunctionOrderUpdate(Guid Id, Guid? ParentId, int SortOrder);

public record CreateRoleRequest
{
    public Guid? OrganizationId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsEnabled { get; init; } = true;
    public List<Guid> FunctionIds { get; init; } = new();
    public List<RoleDataScopeDto> DataScopes { get; init; } = new();
}

public record RoleDataScopeDto
{
    public string EntityName { get; init; } = string.Empty;
    public string ScopeType { get; init; } = RoleDataScopeTypes.All;
    public string? FilterExpression { get; init; }
}

public record AssignRoleRequest
{
    public string UserId { get; init; } = string.Empty;
    public Guid RoleId { get; init; }
    public Guid? OrganizationId { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
}
