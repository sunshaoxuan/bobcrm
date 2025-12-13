using BobCrm.Api.Contracts.Responses.Entity;
using BobCrm.Api.Extensions;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BobCrm.Api.Services;

/// <summary>
/// 字段元数据缓存接口
/// </summary>
public interface IFieldMetadataCache
{
    /// <summary>
    /// 获取实体的字段元数据列表
    /// </summary>
    /// <param name="fullTypeName">实体的完整类型名</param>
    /// <param name="loc">本地化服务</param>
    /// <param name="lang">目标语言代码（null 表示返回多语字典模式）</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>字段元数据 DTO 列表</returns>
    Task<IReadOnlyList<FieldMetadataDto>> GetFieldsAsync(
        string fullTypeName,
        ILocalization loc,
        string? lang,
        CancellationToken ct = default);

    /// <summary>
    /// 使指定实体的字段元数据缓存失效
    /// </summary>
    /// <param name="fullTypeName">实体的完整类型名</param>
    void Invalidate(string fullTypeName);
}

/// <summary>
/// 基于 IMemoryCache 的字段元数据缓存实现
/// </summary>
/// <remarks>
/// 使用滑动过期（30分钟）和绝对过期（2小时）策略，
/// 按 fullTypeName + lang 组合键缓存，支持多语模式与单语模式。
/// </remarks>
public class FieldMetadataCache : IFieldMetadataCache
{
    private const string CacheKeyPrefix = "FieldMetadata:";
    private const string CacheKeySetPrefix = "FieldMetadata:Keys:";
    private static readonly TimeSpan CacheSlidingExpiration = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan CacheAbsoluteExpiration = TimeSpan.FromHours(2);

    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FieldMetadataCache> _logger;

    public FieldMetadataCache(AppDbContext db, IMemoryCache cache, ILogger<FieldMetadataCache> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<FieldMetadataDto>> GetFieldsAsync(
        string fullTypeName,
        ILocalization loc,
        string? lang,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fullTypeName))
        {
            return Array.Empty<FieldMetadataDto>();
        }

        var normalizedType = fullTypeName.Trim();
        var normalizedLang = string.IsNullOrWhiteSpace(lang) ? null : lang.Trim().ToLowerInvariant();

        var cacheKey = BuildCacheKey(normalizedType, normalizedLang, loc);

        return await _cache.GetOrCreateAsync<IReadOnlyList<FieldMetadataDto>>(cacheKey, async entry =>
        {
            entry.SetSlidingExpiration(CacheSlidingExpiration);
            entry.SetAbsoluteExpiration(CacheAbsoluteExpiration);

            TrackCacheKey(normalizedType, cacheKey);

            var definition = await _db.EntityDefinitions
                .AsNoTracking()
                .Include(ed => ed.Fields)
                .FirstOrDefaultAsync(ed => ed.FullTypeName == normalizedType, ct);

            if (definition == null)
            {
                _logger.LogWarning("[FieldMetadataCache] EntityDefinition not found: {FullTypeName}", normalizedType);
                return Array.Empty<FieldMetadataDto>();
            }

            var fields = definition.Fields
                .Where(f => !f.IsDeleted)
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.PropertyName, StringComparer.OrdinalIgnoreCase)
                .Select(f => f.ToFieldDto(loc, normalizedLang))
                .ToList();

            return fields;
        }) ?? Array.Empty<FieldMetadataDto>();
    }

    public void Invalidate(string fullTypeName)
    {
        if (string.IsNullOrWhiteSpace(fullTypeName))
        {
            return;
        }

        var normalizedType = fullTypeName.Trim();
        var keySetKey = $"{CacheKeySetPrefix}{normalizedType}";

        if (_cache.TryGetValue(keySetKey, out HashSet<string>? keys) && keys != null)
        {
            lock (keys)
            {
                foreach (var key in keys)
                {
                    _cache.Remove(key);
                }

                keys.Clear();
            }
        }

        _cache.Remove(keySetKey);
        _cache.Remove($"{CacheKeyPrefix}{normalizedType}");
    }

    private static string BuildCacheKey(string fullTypeName, string? lang, ILocalization loc)
    {
        if (lang == null)
        {
            return $"{CacheKeyPrefix}{fullTypeName}";
        }

        var version = loc.GetCacheVersion();
        return $"{CacheKeyPrefix}{fullTypeName}:{lang}:{version}";
    }

    private void TrackCacheKey(string fullTypeName, string cacheKey)
    {
        var keySetKey = $"{CacheKeySetPrefix}{fullTypeName}";
        var set = _cache.GetOrCreate(keySetKey, entry =>
        {
            entry.SetSlidingExpiration(CacheSlidingExpiration);
            entry.SetAbsoluteExpiration(CacheAbsoluteExpiration);
            return new HashSet<string>(StringComparer.Ordinal);
        });

        if (set == null)
        {
            return;
        }

        lock (set)
        {
            set.Add(cacheKey);
        }
    }
}
