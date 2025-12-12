namespace BobCrm.Api.Contracts.Requests.Auth;

/// <summary>
/// 登录请求
/// </summary>
public record LoginRequest(string Username, string Password);
