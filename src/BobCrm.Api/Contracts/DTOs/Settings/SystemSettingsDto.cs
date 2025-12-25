namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 系统设置 DTO
/// </summary>
public record SystemSettingsDto(
    string CompanyName,
    string DefaultTheme,
    string? DefaultPrimaryColor,
    string DefaultLanguage,
    string DefaultHomeRoute,
    string DefaultNavDisplayMode,
    string TimeZoneId,
    bool AllowSelfRegistration,
    string? SmtpHost,
    int SmtpPort,
    string? SmtpUsername,
    bool SmtpEnableSsl,
    string? SmtpFromAddress,
    string? SmtpDisplayName,
    bool SmtpPasswordConfigured);
