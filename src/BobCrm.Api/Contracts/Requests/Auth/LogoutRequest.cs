namespace BobCrm.Api.Contracts.Requests.Auth;

/// <summary>
/// 登出请求
/// </summary>
public record LogoutRequest(string RefreshToken);
