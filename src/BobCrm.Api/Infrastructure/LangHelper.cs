using Microsoft.AspNetCore.Http;
using System.Text.Json;

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

    public static string? ResolveDisplayName(string? key, string? rawJson, string? targetLang, ILocalization loc)
    {
        if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(targetLang))
        {
            var translated = loc.T(key.Trim(), targetLang.Trim().ToLowerInvariant());
            if (!string.IsNullOrWhiteSpace(translated) && !string.Equals(translated, key, StringComparison.OrdinalIgnoreCase))
            {
                return translated;
            }
        }

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return null;
        }

        var trimmed = rawJson.Trim();
        if (trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (!string.IsNullOrWhiteSpace(targetLang) &&
                        doc.RootElement.TryGetProperty(targetLang.Trim().ToLowerInvariant(), out var targetValue) &&
                        targetValue.ValueKind == JsonValueKind.String)
                    {
                        return targetValue.GetString();
                    }

                    if (doc.RootElement.TryGetProperty("en", out var enValue) && enValue.ValueKind == JsonValueKind.String)
                    {
                        return enValue.GetString();
                    }

                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.String)
                        {
                            var value = prop.Value.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                return value;
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignore parse errors, fallback below
            }
        }

        return rawJson;
    }
}
