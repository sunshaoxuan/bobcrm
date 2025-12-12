namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 更新用户设置请求
/// </summary>
public record UpdateUserSettingsRequest(
    string? Theme,
    string? PrimaryColor,
    string? Language,
    string? HomeRoute,
    string? NavDisplayMode);
