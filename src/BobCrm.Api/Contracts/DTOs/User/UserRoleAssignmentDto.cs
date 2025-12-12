namespace BobCrm.Api.Contracts.DTOs.User;

/// <summary>
/// 用户角色关联 DTO
/// </summary>
public record UserRoleAssignmentDto
{
    public Guid RoleId { get; init; }
    public string RoleCode { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public Guid? OrganizationId { get; init; }
}
