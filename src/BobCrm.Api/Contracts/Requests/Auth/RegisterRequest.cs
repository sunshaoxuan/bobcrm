namespace BobCrm.Api.Contracts.Requests.Auth;

/// <summary>
/// 注册请求
/// </summary>
public record RegisterRequest(string Username, string Password, string Email);
