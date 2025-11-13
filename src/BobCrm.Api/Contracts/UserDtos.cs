using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Contracts.DTOs;

public record UserSummaryDto
{
    public string Id { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool EmailConfirmed { get; init; }
    public bool IsLocked { get; init; }
    public List<UserRoleAssignmentDto> Roles { get; init; } = new();
}

public record UserDetailDto : UserSummaryDto
{
    public string? PhoneNumber { get; init; }
    public bool TwoFactorEnabled { get; init; }
    public DateTimeOffset? LockoutEnd { get; init; }
}

public record CreateUserRequest
{
    [Required]
    public string UserName { get; init; } = string.Empty;
    [Required]
    public string Email { get; init; } = string.Empty;
    public string? Password { get; init; }
    public bool EmailConfirmed { get; init; } = true;
    public List<UserRoleAssignmentRequest> Roles { get; init; } = new();
}

public record UpdateUserRequest
{
    public string? Email { get; init; }
    public bool? EmailConfirmed { get; init; }
    public string? PhoneNumber { get; init; }
    public bool? IsLocked { get; init; }
    public string? Password { get; init; }
}

public record UpdateUserRolesRequest
{
    public List<UserRoleAssignmentRequest> Roles { get; init; } = new();
}

public record UserRoleAssignmentDto
{
    public Guid RoleId { get; init; }
    public string RoleCode { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public Guid? OrganizationId { get; init; }
}

public record UserRoleAssignmentRequest
{
    public Guid RoleId { get; init; }
    public Guid? OrganizationId { get; init; }
}
