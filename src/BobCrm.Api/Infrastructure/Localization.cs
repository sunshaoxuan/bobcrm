using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using BobCrm.Api.Domain;

namespace BobCrm.Api.Infrastructure;

public static class LangHelper
{
    public static string GetLang(HttpContext http) =>
        (http.Request.Headers["X-Lang"].FirstOrDefault() ?? http.Request.Query["lang"].FirstOrDefault() ?? "ja").ToLowerInvariant();
}

public interface ILocalization
{
    string T(string key, string lang);
}

public class EfLocalization(DbContext db) : ILocalization
{
    private readonly Dictionary<string, Dictionary<string, string>> _cache = new(StringComparer.OrdinalIgnoreCase);

    private Dictionary<string, string> EnsureLoaded(string lang)
    {
        lang = (lang ?? "ja").ToLowerInvariant();
        if (_cache.TryGetValue(lang, out var dict)) return dict;
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var query = db.Set<LocalizationResource>().AsNoTracking().ToList();
        foreach (var r in query)
        {
            var val = lang switch
            {
                "ja" => r.JA ?? r.ZH ?? r.EN ?? r.Key,
                "en" => r.EN ?? r.JA ?? r.ZH ?? r.Key,
                "zh" => r.ZH ?? r.JA ?? r.EN ?? r.Key,
                _ => r.JA ?? r.ZH ?? r.EN ?? r.Key
            };
            map[r.Key] = val;
        }
        _cache[lang] = map;
        return map;
    }
    public string T(string key, string lang)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        var dict = EnsureLoaded(lang);
        return dict.TryGetValue(key, out var v) ? v : key;
    }
}
