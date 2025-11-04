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
/// </summary>
public class EfLocalization : ILocalization
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private const string CacheKeyPrefix = "i18n_";
    private const string VersionCacheKey = "i18n_version";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(30);

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
                var val = lang switch
                {
                    "ja" => r.JA ?? r.ZH ?? r.EN ?? r.Key,
                    "en" => r.EN ?? r.JA ?? r.ZH ?? r.Key,
                    "zh" => r.ZH ?? r.JA ?? r.EN ?? r.Key,
                    _ => r.JA ?? r.ZH ?? r.EN ?? r.Key
                };
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
        // 移除所有语言的缓存
        var languages = new[] { "ja", "zh", "en" };
        foreach (var lang in languages)
        {
            _cache.Remove($"{CacheKeyPrefix}{lang}");
        }

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
