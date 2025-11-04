using Microsoft.EntityFrameworkCore;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Domain;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// 国际化/多语言相关端点
/// </summary>
public static class I18nEndpoints
{
    public static IEndpointRouteBuilder MapI18nEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/i18n")
            .WithTags("国际化")
            .WithOpenApi();

        // 获取当前缓存版本（用于客户端版本检查）
        group.MapGet("/version", (ILocalization loc) =>
        {
            var version = loc.GetCacheVersion();
            return Results.Json(new { version });
        })
        .WithName("GetI18nVersion")
        .WithSummary("获取多语资源版本")
        .WithDescription("客户端用于检查本地缓存是否需要更新");

        // 获取所有多语资源（管理用）
        group.MapGet("/resources", (
            AppDbContext db,
            ILocalization loc,
            HttpContext http,
            ILogger<Program> logger) =>
        {
            var version = loc.GetCacheVersion().ToString();
            var etag = $"\"{version}\"";

            // 检查 If-None-Match 头
            if (http.Request.Headers.TryGetValue("If-None-Match", out var clientEtag) && clientEtag == etag)
            {
                logger.LogDebug("[I18n] Resources not modified, returning 304");
                return Results.StatusCode(304); // Not Modified
            }

            var list = db.LocalizationResources.AsNoTracking().ToList();

            // 设置 ETag 和 Cache-Control 头
            http.Response.Headers["ETag"] = etag;
            http.Response.Headers["Cache-Control"] = "public, max-age=1800"; // 30 分钟

            logger.LogDebug("[I18n] Returning {Count} resources with ETag {ETag}", list.Count, etag);
            return Results.Json(list);
        })
        .RequireAuthorization()
        .WithName("GetI18nResources")
        .WithSummary("获取所有多语资源")
        .WithDescription("管理用：获取完整的多语资源列表，支持ETag缓存");

        // 获取指定语言的字典
        group.MapGet("/{lang}", (
            string lang,
            ILocalization loc,
            HttpContext http,
            ILogger<Program> logger) =>
        {
            lang = (lang ?? "ja").ToLowerInvariant();
            var version = loc.GetCacheVersion().ToString();
            var etag = $"\"{version}_{lang}\"";

            // 检查 If-None-Match 头
            if (http.Request.Headers.TryGetValue("If-None-Match", out var clientEtag) && clientEtag == etag)
            {
                logger.LogDebug("[I18n] Language dictionary not modified for {Lang}, returning 304", lang);
                return Results.StatusCode(304); // Not Modified
            }

            // 使用 ILocalization 获取缓存的字典（统一数据源）
            var dict = loc.GetDictionary(lang);

            // 设置 ETag 和 Cache-Control 头
            http.Response.Headers["ETag"] = etag;
            http.Response.Headers["Cache-Control"] = "public, max-age=1800"; // 30 分钟

            logger.LogDebug("[I18n] Returning {Count} entries for language {Lang} with ETag {ETag}", 
                dict.Count, lang, etag);
            return Results.Json(dict);
        })
        .WithName("GetLanguageDictionary")
        .WithSummary("获取指定语言字典")
        .WithDescription("获取指定语言的键值对字典，支持ETag缓存");

        // 获取可用语言列表
        group.MapGet("/languages", (AppDbContext db, ILogger<Program> logger) =>
        {
            var list = db.LocalizationLanguages.AsNoTracking()
                .Select(l => new { code = l.Code, name = l.NativeName })
                .ToList();

            if (list.Count == 0)
            {
                logger.LogInformation("[I18n] No languages in database, returning defaults");
                list = new[]
                {
                    new { code = "ja", name = "日本語" },
                    new { code = "zh", name = "中文" },
                    new { code = "en", name = "English" }
                }.ToList();
            }
            
            logger.LogDebug("[I18n] Returning {Count} available languages", list.Count);
            return Results.Json(list);
        })
        .WithName("GetLanguages")
        .WithSummary("获取可用语言列表")
        .WithDescription("获取系统支持的所有语言及其本地化名称");

        return app;
    }
}
