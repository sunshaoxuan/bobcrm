namespace BobCrm.Api.Contracts.Requests.User;

/// <summary>
/// 更新用户角色请求
/// </summary>
public record UpdateUserRolesRequest
{
    public List<UserRoleAssignmentRequest> Roles { get; init; } = new();
}
