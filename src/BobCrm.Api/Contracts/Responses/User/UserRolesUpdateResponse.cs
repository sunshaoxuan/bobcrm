using UserRoleDetailsDto = BobCrm.Api.Contracts.DTOs.User.UserRoleAssignmentDto;

namespace BobCrm.Api.Contracts.Responses.User;

/// <summary>
/// 用户角色更新响应 DTO。
/// </summary>
public class UserRolesUpdateResponse
{
    /// <summary>
    /// 更新是否成功。
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 更新后的角色明细列表。
    /// </summary>
    public List<UserRoleDetailsDto> Roles { get; set; } = new();
}

