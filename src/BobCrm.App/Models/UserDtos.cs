using System;
using System.Collections.Generic;

namespace BobCrm.App.Models;

public class UserSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsLocked { get; set; }
    public List<UserRoleAssignmentDto> Roles { get; set; } = new();
}

public class UserDetailDto : UserSummaryDto
{
    public string? PhoneNumber { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
}

public class CreateUserRequestDto
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public bool EmailConfirmed { get; set; } = true;
    public List<UserRoleAssignmentRequestDto> Roles { get; set; } = new();
}

public class UpdateUserRequestDto
{
    public string? Email { get; set; }
    public bool? EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public bool? IsLocked { get; set; }
    public string? Password { get; set; }
}

public class UpdateUserRolesRequestDto
{
    public List<UserRoleAssignmentRequestDto> Roles { get; set; } = new();
}

public class UserRoleAssignmentDto
{
    public Guid RoleId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
}

public class UserRoleAssignmentRequestDto
{
    public Guid RoleId { get; set; }
    public Guid? OrganizationId { get; set; }
}
