using System;
using System.Collections.Generic;

namespace BobCrm.App.Models;

public class RoleProfileDto
{
    public Guid Id { get; set; }
    public Guid? OrganizationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<RoleFunctionDto> Functions { get; set; } = new();
    public List<RoleDataScopeDto> DataScopes { get; set; } = new();
}

public class RoleFunctionDto
{
    public Guid RoleId { get; set; }
    public Guid FunctionId { get; set; }
    public FunctionMenuNode? Function { get; set; }
    public int? TemplateBindingId { get; set; }
}

public class RoleDataScopeDto
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string ScopeType { get; set; } = string.Empty;
    public string? FilterExpression { get; set; }
}

public class CreateRoleRequestDto
{
    public Guid? OrganizationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<Guid> FunctionIds { get; set; } = new();
    public List<RoleDataScopeDto> DataScopes { get; set; } = new();
}

public class UpdateRoleRequestDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsEnabled { get; set; }
}

public class UpdatePermissionsRequestDto
{
    public List<Guid> FunctionIds { get; set; } = new();
    public List<RoleDataScopeDto> DataScopes { get; set; } = new();
    public List<FunctionPermissionSelectionDto> FunctionPermissions { get; set; } = new();
}

public class FunctionPermissionSelectionDto
{
    public Guid FunctionId { get; set; }
    public int? TemplateBindingId { get; set; }
}
