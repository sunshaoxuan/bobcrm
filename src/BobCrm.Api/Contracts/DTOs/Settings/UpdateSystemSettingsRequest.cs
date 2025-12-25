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
    bool? AllowSelfRegistration,
    string? SmtpHost,
    int? SmtpPort,
    string? SmtpUsername,
    string? SmtpPassword,
    bool? SmtpEnableSsl,
    string? SmtpFromAddress,
    string? SmtpDisplayName);
