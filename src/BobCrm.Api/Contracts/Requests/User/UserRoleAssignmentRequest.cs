namespace BobCrm.Api.Contracts.Requests.User;

/// <summary>
/// 用户角色关联请求
/// </summary>
public record UserRoleAssignmentRequest
{
    public Guid RoleId { get; init; }
    public Guid? OrganizationId { get; init; }
}
