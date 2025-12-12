namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 更新系统设置请求
/// </summary>
public record UpdateSystemSettingsRequest(
    string? CompanyName,
    string? DefaultTheme,
    string? DefaultPrimaryColor,
    string? DefaultLanguage,
    string? DefaultHomeRoute,
    string? DefaultNavDisplayMode,
    string? TimeZoneId,
    bool? AllowSelfRegistration);
