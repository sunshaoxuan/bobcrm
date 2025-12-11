namespace BobCrm.Api.Contracts.Requests.Access;

/// <summary>
/// 更新角色基础信息请求
/// </summary>
public record UpdateRoleRequest(string? Name, string? Description, bool? IsEnabled);
