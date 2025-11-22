using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BobCrm.Api.Services;

/// <summary>
/// 提供多语言字段的读取与合并功能，便于在服务层处理 jsonb 字段。
/// </summary>
public class MultilingualFieldService
{
    private readonly AppDbContext _db;
    private readonly ILogger<MultilingualFieldService> _logger;

    public MultilingualFieldService(AppDbContext db, ILogger<MultilingualFieldService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// 根据多语键与显式字典解析最终的多语言数据。
    /// </summary>
    /// <param name="resourceKey">多语资源键，可为空。</param>
    /// <param name="explicitValues">显式提供的字典，会覆盖资源中的非空值。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>标准化后的多语言字典，或 null。</returns>
    public async Task<Dictionary<string, string?>?> ResolveAsync(
        string? resourceKey,
        Dictionary<string, string?>? explicitValues,
        CancellationToken ct = default)
    {
        // 1. Normalize the fallback/explicit values
        var normalizedFallback = Normalize(explicitValues);

        if (string.IsNullOrWhiteSpace(resourceKey))
        {
            return normalizedFallback;
        }

        // 2. Load resource from database
        var resource = await LoadResourceAsync(resourceKey.Trim(), ct);
        
        // 3. If no DB resource, return fallback
        if (resource == null)
        {
            if (normalizedFallback != null && normalizedFallback.Count > 0)
            {
                return normalizedFallback;
            }
            
            _logger.LogWarning("[i18n] Localization resource '{ResourceKey}' not found and no fallback provided.", resourceKey);
            return null;
        }

        // 4. Merge using the centralized logic
        return Merge(resource, normalizedFallback);
    }

    /// <summary>
    /// 批量加载多语资源。
    /// </summary>
    public async Task<Dictionary<string, Dictionary<string, string?>>> LoadResourcesAsync(
        IEnumerable<string> keys,
        CancellationToken ct = default)
    {
        var distinctKeys = keys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctKeys.Count == 0)
        {
            return new Dictionary<string, Dictionary<string, string?>>();
        }

        var resources = await _db.LocalizationResources
            .AsNoTracking()
            .Where(r => distinctKeys.Contains(r.Key))
            .ToListAsync(ct);

        var result = new Dictionary<string, Dictionary<string, string?>>(StringComparer.OrdinalIgnoreCase);
        foreach (var resource in resources)
        {
             result[resource.Key] = resource.Translations
                .ToDictionary(
                    pair => pair.Key.Trim().ToLowerInvariant(),
                    pair => string.IsNullOrWhiteSpace(pair.Value) ? null : pair.Value.Trim(),
                    StringComparer.OrdinalIgnoreCase);
        }

        return result;
    }

    /// <summary>
    /// 合并多语资源：数据库资源优先于代码默认值。
    /// </summary>
    public Dictionary<string, string?>? Merge(
        Dictionary<string, string?>? resource,
        Dictionary<string, string?>? fallback)
    {
        // If no fallback, return resource (even if null)
        if (fallback == null || fallback.Count == 0)
        {
            return resource;
        }

        // If no resource, return fallback
        if (resource == null || resource.Count == 0)
        {
            return fallback;
        }

        // Merge: Start with fallback, then overlay DB values (DB overrides code)
        var result = new Dictionary<string, string?>(fallback, StringComparer.OrdinalIgnoreCase);
        
        foreach (var (lang, text) in resource)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                result[lang] = text;
            }
        }

        return result;
    }

    /// <summary>
    /// 将输入字典标准化：
    /// - 去除空键/空值
    /// - 统一语言代码为小写
    /// - 使用 OrdinalIgnoreCase 比较器
    /// </summary>
    public static Dictionary<string, string?>? Normalize(Dictionary<string, string?>? source)
    {
        if (source == null || source.Count == 0)
        {
            return null;
        }

        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in source)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key))
            {
                continue;
            }

            var lang = kvp.Key.Trim().ToLowerInvariant();
            var value = kvp.Value?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            result[lang] = value;
        }

        return result.Count == 0 ? null : result;
    }

    /// <summary>
    /// 根据单一语言值创建多语字典。
    /// </summary>
    public static Dictionary<string, string?>? FromSingleValue(string? value, string language = "zh")
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var lang = string.IsNullOrWhiteSpace(language) ? "zh" : language.Trim().ToLowerInvariant();
        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [lang] = value.Trim()
        };
    }

    private async Task<Dictionary<string, string?>?> LoadResourceAsync(string resourceKey, CancellationToken ct)
    {
        var resource = await _db.LocalizationResources
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == resourceKey, ct);

        if (resource == null)
        {
            return null;
        }

        return resource.Translations
            .ToDictionary(
                pair => pair.Key.Trim().ToLowerInvariant(),
                pair => string.IsNullOrWhiteSpace(pair.Value) ? null : pair.Value.Trim(),
                StringComparer.OrdinalIgnoreCase);
    }
}
