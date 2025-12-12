namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 用户设置 DTO
/// </summary>
public record UserSettingsDto(
    string Theme,
    string? PrimaryColor,
    string Language,
    string HomeRoute,
    string NavDisplayMode);
