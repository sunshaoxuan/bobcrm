namespace BobCrm.Api.Contracts.DTOs;

public record UserPreferencesDto(
    string? theme,
    string? language,
    string? udfColor,
    string? homeRoute,
    string? navMode);

