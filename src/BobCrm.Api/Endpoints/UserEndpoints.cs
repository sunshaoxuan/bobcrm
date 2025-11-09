using System.Security.Claims;
using BobCrm.Api.Contracts.DTOs;
using Microsoft.AspNetCore.Routing;
using BobCrm.Api.Services.Settings;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// 用户与偏好接口（遗留 Theme API + 新增设置快照透出）
/// </summary>
public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var themeGroup = app.MapGroup("/api/theme")
            .WithTags("Theme")
            .WithOpenApi();

        var userGroup = app.MapGroup("/api/user")
            .WithTags("Users")
            .WithOpenApi()
            .RequireAuthorization();

        MapThemeDefaults(themeGroup);
        MapLegacyPreferences(userGroup);

        return app;
    }

    private static void MapThemeDefaults(IEndpointRouteBuilder themeGroup)
    {
        themeGroup.MapGet("/defaults", (IConfiguration cfg, ILogger<Program> logger) =>
        {
            var initColor = cfg.GetValue<string>("Theme:InitColor");
            var initTheme = cfg.GetValue<string>("Theme:InitTheme") ?? "light";
            logger.LogDebug("[Theme] Defaults requested: theme={Theme}, color={Color}", initTheme, initColor);
            return Results.Json(new { initColor, initTheme });
        })
        .WithName("GetThemeDefaults")
        .WithSummary("获取主题默认值")
        .WithDescription("供早期客户端快速读取开箱即用的主题信息")
        .AllowAnonymous();
    }

    private static void MapLegacyPreferences(RouteGroupBuilder userGroup)
    {
        // Legacy preference GET (kept for backward compatibility)
        userGroup.MapGet("/preferences", async (
            ClaimsPrincipal user,
            SettingsService settings,
            IConfiguration cfg,
            ILogger<Program> logger) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid))
            {
                logger.LogWarning("[Preferences] Unauthorized access attempt");
                return Results.Unauthorized();
            }

            var snapshot = await settings.GetUserSettingsAsync(uid);
            logger.LogDebug("[Preferences] Snapshot served for user {UserId}", uid);
            return Results.Json(ToLegacyPreferences(snapshot, cfg));
        })
        .WithName("GetUserPreferences")
        .WithSummary("获取用户偏好（兼容旧接口）")
        .WithDescription("返回主题/颜色/语言等信息，底层已统一走 SettingsService");

        // Legacy preference PUT (writes via SettingsService to keep single source of truth)
        userGroup.MapPut("/preferences", async (
            UserPreferencesDto dto,
            ClaimsPrincipal user,
            SettingsService settings,
            IConfiguration cfg,
            ILogger<Program> logger) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid))
            {
                logger.LogWarning("[Preferences] Unauthorized update attempt");
                return Results.Unauthorized();
            }

            logger.LogInformation("[Preferences] Updating preferences for user {UserId}", uid);
            var snapshot = await settings.UpdateUserSettingsAsync(uid, new UpdateUserSettingsRequest(
                dto.theme,
                dto.udfColor,
                dto.language,
                dto.homeRoute,
                dto.navMode));

            return Results.Json(ToLegacyPreferences(snapshot, cfg));
        })
        .WithName("UpdateUserPreferences")
        .WithSummary("更新用户偏好（兼容旧接口）")
        .WithDescription("向新的设置管道写入，同时保持旧数据结构响应");
    }

    private static object ToLegacyPreferences(UserSettingsSnapshotDto snapshot, IConfiguration cfg)
    {
        var fallbackColor = snapshot.Effective.PrimaryColor
                            ?? snapshot.System.DefaultPrimaryColor
                            ?? cfg.GetValue<string>("Theme:InitColor");

        var language = string.IsNullOrWhiteSpace(snapshot.Effective.Language)
            ? snapshot.System.DefaultLanguage
            : snapshot.Effective.Language;

        return new
        {
            theme = snapshot.Effective.Theme,
            udfColor = fallbackColor,
            language,
            homeRoute = snapshot.Effective.HomeRoute,
            navMode = snapshot.Effective.NavDisplayMode
        };
    }
}
