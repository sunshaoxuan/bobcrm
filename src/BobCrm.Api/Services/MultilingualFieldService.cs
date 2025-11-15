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
        var normalized = Normalize(explicitValues);

        if (string.IsNullOrWhiteSpace(resourceKey))
        {
            return normalized;
        }

        var resource = await LoadResourceAsync(resourceKey.Trim(), ct);
        if (resource == null)
        {
            _logger.LogWarning("[i18n] Localization resource '{ResourceKey}' not found when resolving multilingual field.", resourceKey);
            return normalized;
        }

        if (normalized == null || normalized.Count == 0)
        {
            return resource;
        }

        foreach (var (lang, text) in resource)
        {
            if (!normalized.TryGetValue(lang, out var existing) || string.IsNullOrWhiteSpace(existing))
            {
                normalized[lang] = text;
            }
        }

        return normalized;
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
