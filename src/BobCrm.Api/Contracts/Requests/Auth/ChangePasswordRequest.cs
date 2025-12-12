namespace BobCrm.Api.Contracts.Requests.Auth;

/// <summary>
/// 修改密码请求
/// </summary>
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
