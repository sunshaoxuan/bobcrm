namespace BobCrm.Api.Contracts.Requests.Auth;

/// <summary>
/// 刷新令牌请求
/// </summary>
public record RefreshRequest(string RefreshToken);
