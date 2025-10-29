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
    public string T(string key, string lang)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        var res = db.Set<LocalizationResource>().AsNoTracking().FirstOrDefault(x => x.Key == key);
        if (res == null) return key;
        return lang switch
        {
            "zh" => res.ZH ?? res.JA ?? res.EN ?? key,
            "en" => res.EN ?? res.JA ?? res.ZH ?? key,
            _ => res.JA ?? res.ZH ?? res.EN ?? key
        };
    }
}

