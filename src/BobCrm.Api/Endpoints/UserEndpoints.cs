using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Domain;
using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// 用户相关端点（偏好设置、主题等）
/// </summary>
public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var themeGroup = app.MapGroup("/api/theme")
            .WithTags("主题")
            .WithOpenApi();

        var userGroup = app.MapGroup("/api/user")
            .WithTags("用户设置")
            .WithOpenApi()
            .RequireAuthorization();

        // 获取主题默认配置（公开访问）
        themeGroup.MapGet("/defaults", (IConfiguration cfg, ILogger<Program> logger) =>
        {
            var initColor = cfg.GetValue<string>("Theme:InitColor");
            var initTheme = cfg.GetValue<string>("Theme:InitTheme") ?? "light";
            logger.LogDebug("[Theme] Defaults requested: theme={Theme}, color={Color}", initTheme, initColor);
            return Results.Json(new { initColor, initTheme });
        })
        .WithName("GetThemeDefaults")
        .WithSummary("获取主题默认配置")
        .WithDescription("获取系统默认的主题和主色配置，无需认证")
        .AllowAnonymous();

        // 获取用户偏好设置
        userGroup.MapGet("/preferences", async (
            AppDbContext db,
            ClaimsPrincipal user,
            IConfiguration cfg,
            ILogger<Program> logger) =>
        {
            var uid = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(uid))
            {
                logger.LogWarning("[Preferences] Unauthorized access attempt");
                return Results.Unauthorized();
            }

            var prefs = await db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == uid);
            if (prefs == null)
            {
                // 返回配置中的默认值
                var initColor = cfg.GetValue<string>("Theme:InitColor");
                var initTheme = cfg.GetValue<string>("Theme:InitTheme") ?? "light";
                logger.LogInformation("[Preferences] User {UserId} has no saved preferences, returning defaults", uid);
                return Results.Json(new { theme = initTheme, udfColor = initColor, language = "ja" });
            }

            logger.LogDebug("[Preferences] Retrieved preferences for user {UserId}: theme={Theme}, color={Color}, lang={Language}", 
                uid, prefs.Theme, prefs.PrimaryColor, prefs.Language);
            return Results.Json(new
            {
                theme = prefs.Theme ?? "light",
                udfColor = prefs.PrimaryColor ?? cfg.GetValue<string>("Theme:InitColor"),
                language = prefs.Language ?? "ja"
            });
        })
        .WithName("GetUserPreferences")
        .WithSummary("获取用户偏好设置")
        .WithDescription("获取当前用户的主题、颜色和语言偏好");

        // 更新用户偏好设置
        userGroup.MapPut("/preferences", async (
            UserPreferencesDto dto,
            AppDbContext db,
            ClaimsPrincipal user,
            ILogger<Program> logger) =>
        {
            var uid = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(uid))
            {
                logger.LogWarning("[Preferences] Unauthorized update attempt");
                return Results.Unauthorized();
            }

            logger.LogInformation("[Preferences] Updating preferences for user {UserId}: theme={Theme}, color={Color}, lang={Lang}", 
                uid, dto.theme, dto.udfColor, dto.language);

            // 使用 Upsert 模式避免并发冲突
            var prefs = await db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == uid);
            if (prefs == null)
            {
                // 创建新记录
                prefs = new UserPreferences 
                { 
                    UserId = uid,
                    Theme = dto.theme ?? "light",
                    PrimaryColor = dto.udfColor ?? "#3f7cff",  // DTO 的 udfColor 映射到数据库的 PrimaryColor
                    Language = dto.language ?? "ja",
                    UpdatedAt = DateTime.UtcNow
                };
                db.UserPreferences.Add(prefs);
                
                try
                {
                    await db.SaveChangesAsync();
                    logger.LogInformation("[Preferences] New preferences created for user {UserId}", uid);
                }
                catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("IX_UserPreferences_UserId") == true)
                {
                    // 并发冲突：其他请求已创建记录，重新查询并更新
                    logger.LogWarning("[Preferences] Concurrent insert detected for user {UserId}, retrying as update", uid);
                    db.Entry(prefs).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    prefs = await db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == uid);
                    if (prefs == null)
                    {
                        logger.LogError("[Preferences] Failed to retrieve preferences after concurrent conflict for user {UserId}", uid);
                        return Results.StatusCode(500);
                    }
                    
                    // 更新现有记录（DTO udfColor → DB PrimaryColor）
                    if (!string.IsNullOrEmpty(dto.theme)) prefs.Theme = dto.theme;
                    if (!string.IsNullOrEmpty(dto.udfColor)) prefs.PrimaryColor = dto.udfColor;
                    if (!string.IsNullOrEmpty(dto.language)) prefs.Language = dto.language;
                    prefs.UpdatedAt = DateTime.UtcNow;
                    
                    await db.SaveChangesAsync();
                    logger.LogInformation("[Preferences] Preferences updated after retry for user {UserId}", uid);
                }
            }
            else
            {
                // 更新现有记录（DTO udfColor → DB PrimaryColor）
                if (!string.IsNullOrEmpty(dto.theme)) prefs.Theme = dto.theme;
                if (!string.IsNullOrEmpty(dto.udfColor)) prefs.PrimaryColor = dto.udfColor;
                if (!string.IsNullOrEmpty(dto.language)) prefs.Language = dto.language;
                prefs.UpdatedAt = DateTime.UtcNow;
                
                await db.SaveChangesAsync();
                logger.LogInformation("[Preferences] Preferences updated successfully for user {UserId}", uid);
            }
            return Results.Json(new
            {
                theme = prefs.Theme,
                udfColor = prefs.PrimaryColor,  // DB PrimaryColor → 响应 udfColor
                language = prefs.Language
            });
        })
        .WithName("UpdateUserPreferences")
        .WithSummary("更新用户偏好设置")
        .WithDescription("更新当前用户的主题、颜色和语言偏好");

        return app;
    }
}
