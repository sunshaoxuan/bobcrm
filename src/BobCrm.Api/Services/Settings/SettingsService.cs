using System.Security.Claims;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Domain;
using BobCrm.Api.Domain.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services.Settings;

public class SettingsService
{
    private static readonly HashSet<string> SupportedThemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "calm-light",
        "calm-dark",
        "theme-calm-light",
        "theme-calm-dark"
    };

    private static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        "zh",
        "ja",
        "en"
    };

    private readonly AppDbContext _db;

    public SettingsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<SystemSettings> GetSystemSettingsAsync() => await EnsureSystemSettingsAsync();

    public async Task<SystemSettings> UpdateSystemSettingsAsync(UpdateSystemSettingsRequest request)
    {
        var entity = await EnsureSystemSettingsAsync();

        if (!string.IsNullOrWhiteSpace(request.CompanyName))
            entity.CompanyName = request.CompanyName.Trim();
        if (request.DefaultTheme is not null)
            entity.DefaultTheme = NormalizeTheme(request.DefaultTheme, entity.DefaultTheme);
        if (request.DefaultPrimaryColor is not null)
            entity.DefaultPrimaryColor = NormalizeColor(request.DefaultPrimaryColor);
        if (request.DefaultLanguage is not null)
            entity.DefaultLanguage = NormalizeLanguage(request.DefaultLanguage, entity.DefaultLanguage);
        if (request.DefaultHomeRoute is not null)
            entity.DefaultHomeRoute = NormalizeHomeRoute(request.DefaultHomeRoute, entity.DefaultHomeRoute);
        if (request.DefaultNavDisplayMode is not null)
            entity.DefaultNavMode = NavDisplayModes.Normalize(request.DefaultNavDisplayMode, entity.DefaultNavMode);
        if (!string.IsNullOrWhiteSpace(request.TimeZoneId))
            entity.TimeZoneId = request.TimeZoneId.Trim();
        if (request.AllowSelfRegistration.HasValue)
            entity.AllowSelfRegistration = request.AllowSelfRegistration.Value;

        entity.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<UserSettingsSnapshotDto> GetUserSettingsAsync(string userId)
    {
        var system = await EnsureSystemSettingsAsync();
        var prefs = await _db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);

        var systemDto = ToSystemDto(system);
        var effective = ComposeEffective(system, prefs);
        var overrides = prefs != null ? ToUserDto(prefs) : null;

        return new UserSettingsSnapshotDto(systemDto, effective, overrides);
    }

    public async Task<UserSettingsSnapshotDto> UpdateUserSettingsAsync(string userId, UpdateUserSettingsRequest request)
    {
        var system = await EnsureSystemSettingsAsync();
        var prefs = await _db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
        if (prefs == null)
        {
            prefs = new UserPreferences
            {
                UserId = userId
            };
            _db.UserPreferences.Add(prefs);
        }

        bool changed = false;

        if (request.Theme is not null)
        {
            prefs.Theme = NormalizeTheme(request.Theme, prefs.Theme ?? system.DefaultTheme);
            changed = true;
        }

        if (request.PrimaryColor is not null)
        {
            prefs.PrimaryColor = NormalizeColor(request.PrimaryColor);
            changed = true;
        }

        if (request.Language is not null)
        {
            prefs.Language = NormalizeLanguage(request.Language, prefs.Language ?? system.DefaultLanguage);
            changed = true;
        }

        if (request.HomeRoute is not null)
        {
            prefs.HomeRoute = NormalizeHomeRoute(request.HomeRoute, prefs.HomeRoute ?? system.DefaultHomeRoute);
            changed = true;
        }

        if (request.NavDisplayMode is not null)
        {
            prefs.NavDisplayMode = NavDisplayModes.Normalize(request.NavDisplayMode, prefs.NavDisplayMode ?? system.DefaultNavMode);
            changed = true;
        }

        if (changed)
        {
            prefs.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return await GetUserSettingsAsync(userId);
    }

    private async Task<SystemSettings> EnsureSystemSettingsAsync()
    {
        var settings = await _db.SystemSettings.FirstOrDefaultAsync();
        if (settings != null)
        {
            return settings;
        }

        settings = new SystemSettings();
        _db.SystemSettings.Add(settings);
        await _db.SaveChangesAsync();
        return settings;
    }

    private static UserSettingsDto ComposeEffective(SystemSettings system, UserPreferences? prefs)
    {
        return new UserSettingsDto(
            NormalizeTheme(prefs?.Theme, system.DefaultTheme),
            prefs?.PrimaryColor ?? system.DefaultPrimaryColor,
            NormalizeLanguage(prefs?.Language, system.DefaultLanguage),
            NormalizeHomeRoute(prefs?.HomeRoute, system.DefaultHomeRoute),
            NavDisplayModes.Normalize(prefs?.NavDisplayMode, system.DefaultNavMode)
        );
    }

    private static SystemSettingsDto ToSystemDto(SystemSettings system) =>
        new(system.CompanyName,
            system.DefaultTheme,
            system.DefaultPrimaryColor,
            system.DefaultLanguage,
            system.DefaultHomeRoute,
            system.DefaultNavMode,
            system.TimeZoneId,
            system.AllowSelfRegistration);

    private static UserSettingsDto ToUserDto(UserPreferences prefs) =>
        new(
            prefs.Theme ?? string.Empty,
            prefs.PrimaryColor,
            prefs.Language ?? string.Empty,
            NormalizeHomeRoute(prefs.HomeRoute, "/"),
            NavDisplayModes.Normalize(prefs.NavDisplayMode));

    private static string NormalizeTheme(string? theme, string fallback)
    {
        if (string.IsNullOrWhiteSpace(theme))
        {
            return fallback;
        }

        var trimmed = theme.Trim();
        if (!SupportedThemes.Contains(trimmed))
        {
            return fallback;
        }

        var normalized = trimmed.StartsWith("theme-", StringComparison.OrdinalIgnoreCase)
            ? trimmed["theme-".Length..]
            : trimmed;

        return normalized.ToLowerInvariant();
    }

    private static string? NormalizeColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        return color.Trim();
    }

    private static string NormalizeLanguage(string? lang, string fallback)
    {
        if (string.IsNullOrWhiteSpace(lang))
        {
            return fallback;
        }

        var normalized = lang.Trim().ToLowerInvariant();
        return SupportedLanguages.Contains(normalized) ? normalized : fallback;
    }

    private static string NormalizeHomeRoute(string? route, string fallback)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return string.IsNullOrWhiteSpace(fallback) ? "/" : fallback;
        }

        var trimmed = route.Trim();
        if (!trimmed.StartsWith("/"))
        {
            trimmed = "/" + trimmed;
        }
        return trimmed;
    }
}
