namespace BobCrm.Api.Contracts.DTOs;

public record SystemSettingsDto(
    string CompanyName,
    string DefaultTheme,
    string? DefaultPrimaryColor,
    string DefaultLanguage,
    string DefaultHomeRoute,
    string DefaultNavDisplayMode,
    string TimeZoneId,
    bool AllowSelfRegistration);

public record UpdateSystemSettingsRequest(
    string? CompanyName,
    string? DefaultTheme,
    string? DefaultPrimaryColor,
    string? DefaultLanguage,
    string? DefaultHomeRoute,
    string? DefaultNavDisplayMode,
    string? TimeZoneId,
    bool? AllowSelfRegistration);

public record UserSettingsDto(
    string Theme,
    string? PrimaryColor,
    string Language,
    string HomeRoute,
    string NavDisplayMode);

public record UpdateUserSettingsRequest(
    string? Theme,
    string? PrimaryColor,
    string? Language,
    string? HomeRoute,
    string? NavDisplayMode);

public record UserSettingsSnapshotDto(
    SystemSettingsDto System,
    UserSettingsDto Effective,
    UserSettingsDto? Overrides);
