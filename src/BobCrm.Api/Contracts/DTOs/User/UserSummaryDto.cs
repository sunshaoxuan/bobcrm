namespace BobCrm.Api.Contracts.DTOs.User;

/// <summary>
/// 用户概要信息 DTO
/// </summary>
public record UserSummaryDto
{
    public string Id { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool EmailConfirmed { get; init; }
    public bool IsLocked { get; init; }
    public List<UserRoleAssignmentDto> Roles { get; init; } = new();
}
