using Microsoft.AspNetCore.Http;

namespace BobCrm.Api.Infrastructure;

public static class LangHelper
{
    public static string GetLang(HttpContext http) =>
        GetLang(http, null) ?? "ja";

    public static string? GetLang(HttpContext http, string? langOverride)
    {
        var candidate = langOverride;

        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = http.Request.Query["lang"].FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = http.Request.Headers["X-Lang"].FirstOrDefault();
        }

        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = ParseAcceptLanguage(http.Request.Headers["Accept-Language"].FirstOrDefault());
        }

        return string.IsNullOrWhiteSpace(candidate)
            ? null
            : candidate.Trim().ToLowerInvariant();
    }

    private static string? ParseAcceptLanguage(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return null;
        }

        var languages = header.Split(',')
            .Select(part => part.Split(';')[0].Trim())
            .Where(part => !string.IsNullOrWhiteSpace(part));

        foreach (var language in languages)
        {
            var normalized = language.Split('-')[0].Trim();
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized.ToLowerInvariant();
            }
        }

        return null;
    }
}
