using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Domain;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// REST endpoints for localization resources.
/// </summary>
public static class I18nEndpoints
{
    public static IEndpointRouteBuilder MapI18nEndpoints(this IEndpointRouteBuilder app)
    {
        var docLang = ResolveDocumentationLanguage(app);
        string Doc(string key) => ResolveDocString(app, docLang, key);

        var group = app.MapGroup("/api/i18n")
            .WithTags("Localization")
            .WithOpenApi();

        group.MapGet("/version", (ILocalization loc) =>
        {
            var version = loc.GetCacheVersion();
            return Results.Json(new { version });
        })
        .WithName("GetI18nVersion")
        .WithSummary(Doc("DOC_I18N_VERSION_SUMMARY"))
        .WithDescription(Doc("DOC_I18N_VERSION_DESCRIPTION"));

        group.MapGet("/resources", (
            AppDbContext db,
            ILocalization loc,
            HttpContext http,
            ILogger<Program> logger) =>
        {
            var version = loc.GetCacheVersion().ToString();
            var etag = $"\"{version}\"";

            if (http.Request.Headers.TryGetValue("If-None-Match", out var clientEtag) && clientEtag == etag)
            {
                logger.LogDebug("[I18n] Resources not modified, returning 304");
                return Results.StatusCode(304); // Not Modified
            }

            var list = db.LocalizationResources.AsNoTracking().ToList();

            http.Response.Headers["ETag"] = etag;
            http.Response.Headers["Cache-Control"] = "public, max-age=1800";

            logger.LogDebug("[I18n] Returning {Count} resources with ETag {ETag}", list.Count, etag);
            return Results.Json(list);
        })
        .RequireAuthorization()
        .WithName("GetI18nResources")
        .WithSummary(Doc("DOC_I18N_RESOURCES_SUMMARY"))
        .WithDescription(Doc("DOC_I18N_RESOURCES_DESCRIPTION"));

        group.MapGet("/{lang?}", async (
            string? lang,
            AppDbContext db,
            ILocalization loc,
            HttpContext http,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            var resolvedLang = await ResolveLanguageAsync(lang, db, http, logger, ct);
            var version = loc.GetCacheVersion().ToString();
            var etag = $"\"{version}_{resolvedLang}\"";

            if (http.Request.Headers.TryGetValue("If-None-Match", out var clientEtag) && clientEtag == etag)
            {
                logger.LogDebug("[I18n] Language dictionary not modified for {Lang}, returning 304", resolvedLang);
                return Results.StatusCode(304); // Not Modified
            }

            var dict = loc.GetDictionary(resolvedLang);

            http.Response.Headers["ETag"] = etag;
            http.Response.Headers["Cache-Control"] = "public, max-age=1800";

            logger.LogDebug("[I18n] Returning {Count} entries for language {Lang} with ETag {ETag}",
                dict.Count, resolvedLang, etag);
            return Results.Json(dict);
        })
        .WithName("GetLanguageDictionary")
        .WithSummary(Doc("DOC_I18N_LANGUAGE_SUMMARY"))
        .WithDescription(Doc("DOC_I18N_LANGUAGE_DESCRIPTION"));

        group.MapGet("/languages", async (AppDbContext db, ILogger<Program> logger, CancellationToken ct) =>
        {
            var list = await db.LocalizationLanguages.AsNoTracking()
                .OrderBy(l => l.Code)
                .Select(l => new { code = l.Code, name = l.NativeName })
                .ToListAsync(ct);

            if (list.Count == 0)
            {
                logger.LogWarning("[I18n] No languages configured in LocalizationLanguages table");
            }
            else
            {
                logger.LogDebug("[I18n] Returning {Count} available languages", list.Count);
            }

            return Results.Json(list);
        })
        .WithName("GetLanguages")
        .WithSummary(Doc("DOC_I18N_LANGUAGES_SUMMARY"))
        .WithDescription(Doc("DOC_I18N_LANGUAGES_DESCRIPTION"));

        return app;
    }

    private static async Task<string> ResolveLanguageAsync(
        string? requestedLang,
        AppDbContext db,
        HttpContext http,
        ILogger logger,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(requestedLang))
        {
            return requestedLang.ToLowerInvariant();
        }

        var headerLang = http.Request.Headers["X-Lang"].FirstOrDefault()
            ?? http.Request.Query["lang"].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(headerLang))
        {
            return headerLang.ToLowerInvariant();
        }

        var fallback = await db.LocalizationLanguages.AsNoTracking()
            .OrderBy(l => l.Id)
            .Select(l => l.Code)
            .FirstOrDefaultAsync(ct);

        if (!string.IsNullOrWhiteSpace(fallback))
        {
            return fallback.ToLowerInvariant();
        }

        logger.LogWarning("[I18n] No localization languages configured; falling back to 'en'");
        return "en";
    }

    private static string ResolveDocumentationLanguage(IEndpointRouteBuilder app)
    {
        using var scope = app.ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var systemLang = db.SystemSettings
            .AsNoTracking()
            .Select(s => s.DefaultLanguage)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(systemLang))
        {
            return systemLang.ToLowerInvariant();
        }

        var fallback = db.LocalizationLanguages
            .AsNoTracking()
            .OrderBy(l => l.Id)
            .Select(l => l.Code)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(fallback) ? "en" : fallback.ToLowerInvariant();
    }

    private static string ResolveDocString(IEndpointRouteBuilder app, string lang, string key)
    {
        using var scope = app.ServiceProvider.CreateScope();
        var localization = scope.ServiceProvider.GetRequiredService<ILocalization>();
        return localization.T(key, lang);
    }
}
