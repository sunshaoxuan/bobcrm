using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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

    /// <summary>
    /// 获取指定语言的完整字典
    /// </summary>
    Dictionary<string, string> GetDictionary(string lang);

    /// <summary>
    /// 清除缓存，在多语资源更新后调用
    /// </summary>
    void InvalidateCache();

    /// <summary>
    /// 获取当前缓存版本号（用于 ETag）
    /// </summary>
    long GetCacheVersion();
}

/// <summary>
/// 基于 IMemoryCache 的多语本地化服务
/// 使用 Singleton 生命周期，支持跨请求缓存
/// 动态语言支持：不硬编码语种，从数据库动态加载
/// </summary>
public class EfLocalization : ILocalization
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private const string CacheKeyPrefix = "i18n_";
    private const string VersionCacheKey = "i18n_version";
    private const string LanguagesCacheKey = "i18n_languages";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);
    private static readonly string[] FallbackLanguages = new[] { "ja", "en", "zh" };

    public EfLocalization(IServiceProvider serviceProvider, IMemoryCache cache)
    {
        _serviceProvider = serviceProvider;
        _cache = cache;

        // 初始化版本号
        if (!_cache.TryGetValue(VersionCacheKey, out _))
        {
            _cache.Set(VersionCacheKey, DateTime.UtcNow.Ticks, CacheExpiration);
        }
    }

    /// <summary>
    /// 获取系统支持的所有语言代码（从数据库动态加载）
    /// </summary>
    private List<string> GetAvailableLanguages()
    {
        if (_cache.TryGetValue(LanguagesCacheKey, out List<string>? cachedLangs) && cachedLangs != null)
        {
            return cachedLangs;
        }

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContext>();
        var languages = db.Set<LocalizationLanguage>()
            .AsNoTracking()
            .Select(l => l.Code.ToLower())
            .ToList();

        if (languages.Count == 0)
        {
            // 数据库中没有语言配置，返回默认值
            languages = FallbackLanguages.ToList();
        }

        _cache.Set(LanguagesCacheKey, languages, CacheExpiration);
        return languages;
    }

    /// <summary>
    /// 从 Translations 字典中获取指定语言的翻译，支持回退逻辑
    /// </summary>
    private string GetTranslation(Dictionary<string, string> translations, string lang, string key)
    {
        // 1. 尝试获取请求的语言
        if (translations.TryGetValue(lang, out var text) && !string.IsNullOrEmpty(text))
        {
            return text;
        }

        // 2. 尝试回退语言（ja → en → zh）
        foreach (var fallbackLang in FallbackLanguages)
        {
            if (fallbackLang.Equals(lang, StringComparison.OrdinalIgnoreCase))
                continue; // 跳过已经尝试过的语言

            if (translations.TryGetValue(fallbackLang, out text) && !string.IsNullOrEmpty(text))
            {
                return text;
            }
        }

        // 3. 返回第一个可用的翻译
        var firstAvailable = translations.Values.FirstOrDefault(v => !string.IsNullOrEmpty(v));
        if (firstAvailable != null)
        {
            return firstAvailable;
        }

        // 4. 最后回退到 Key 本身
        return key;
    }

    private Dictionary<string, string> EnsureLoaded(string lang)
    {
        lang = (lang ?? "ja").ToLowerInvariant();
        var cacheKey = $"{CacheKeyPrefix}{lang}";

        // 尝试从缓存获取
        if (_cache.TryGetValue(cacheKey, out Dictionary<string, string>? dict) && dict != null)
        {
            return dict;
        }

        // 缓存未命中，从数据库加载
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // 使用 IServiceProvider 创建临时 scope 来获取 DbContext
        // 因为 EfLocalization 是 Singleton，不能直接注入 Scoped 的 DbContext
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DbContext>();
            var query = db.Set<LocalizationResource>().AsNoTracking().ToList();

            foreach (var r in query)
            {
                // ✅ 动态从 Translations 字典查找翻译，不硬编码语种
                var val = GetTranslation(r.Translations, lang, r.Key);
                map[r.Key] = val;
            }
        }

        // 写入缓存
        _cache.Set(cacheKey, map, CacheExpiration);
        return map;
    }

    public string T(string key, string lang)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        var dict = EnsureLoaded(lang);
        return dict.TryGetValue(key, out var v) ? v : key;
    }

    public Dictionary<string, string> GetDictionary(string lang)
    {
        return EnsureLoaded(lang);
    }

    public void InvalidateCache()
    {
        // ✅ 动态清除所有语言的缓存，不硬编码语言列表
        var languages = GetAvailableLanguages();
        foreach (var lang in languages)
        {
            _cache.Remove($"{CacheKeyPrefix}{lang}");
        }

        // 清除语言列表缓存
        _cache.Remove(LanguagesCacheKey);

        // 更新版本号
        _cache.Set(VersionCacheKey, DateTime.UtcNow.Ticks, CacheExpiration);
    }

    public long GetCacheVersion()
    {
        if (_cache.TryGetValue(VersionCacheKey, out long version))
        {
            return version;
        }

        // 如果版本号不存在，初始化并返回
        var newVersion = DateTime.UtcNow.Ticks;
        _cache.Set(VersionCacheKey, newVersion, CacheExpiration);
        return newVersion;
    }
}
