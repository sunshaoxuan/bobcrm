namespace BobCrm.Api.Contracts.DTOs.User;

/// <summary>
/// 用户详细信息 DTO
/// </summary>
public record UserDetailDto : UserSummaryDto
{
    public string? PhoneNumber { get; init; }
    public bool TwoFactorEnabled { get; init; }
    public DateTimeOffset? LockoutEnd { get; init; }
}
