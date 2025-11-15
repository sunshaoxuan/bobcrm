using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Contracts.DTOs;

public record FunctionNodeDto
{
    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Route { get; init; }
    public string? Icon { get; init; }
    public bool IsMenu { get; init; }
    public int SortOrder { get; init; }
    public List<FunctionNodeDto> Children { get; init; } = new();
}

public record CreateFunctionRequest
{
    public Guid? ParentId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Route { get; init; }
    public string? Icon { get; init; }
    public bool IsMenu { get; init; } = true;
    public int SortOrder { get; init; } = 100;
}

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
