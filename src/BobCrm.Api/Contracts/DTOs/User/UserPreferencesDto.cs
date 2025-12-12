namespace BobCrm.Api.Contracts.DTOs.User;

/// <summary>
/// 用户偏好设置 DTO
/// </summary>
public record UserPreferencesDto(
    string? theme,
    string? language,
    string? udfColor,
    string? homeRoute,
    string? navMode);
