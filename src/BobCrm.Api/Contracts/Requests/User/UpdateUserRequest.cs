namespace BobCrm.Api.Contracts.Requests.User;

/// <summary>
/// 更新用户请求
/// </summary>
public record UpdateUserRequest
{
    public string? Email { get; init; }
    public bool? EmailConfirmed { get; init; }
    public string? PhoneNumber { get; init; }
    public bool? IsLocked { get; init; }
    public string? Password { get; init; }
}
