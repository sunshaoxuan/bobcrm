using BobCrm.Api.Domain;
using BobCrm.Api.Domain.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

/// <summary>
/// 元数据国际化服务
/// 负责管理实体定义和字段的多语言资源
/// 使用动态语言支持，从 LocalizationLanguages 表获取可用语言列表
/// </summary>
public class MetadataI18nService
{
    private readonly AppDbContext _db;
    private readonly ILogger<MetadataI18nService> _logger;
    private readonly ILocalization _localization;

    public MetadataI18nService(
        AppDbContext db,
        ILogger<MetadataI18nService> logger,
        ILocalization localization)
    {
        _db = db;
        _logger = logger;
        _localization = localization;
    }

    #region Language Management

    /// <summary>
    /// 获取系统支持的所有语言代码
    /// </summary>
    public async Task<List<string>> GetAvailableLanguagesAsync()
    {
        var languages = await _db.LocalizationLanguages
            .AsNoTracking()
            .Select(l => l.Code.ToLower())
            .ToListAsync();

        // 如果没有配置语言，返回默认的三种语言
        if (!languages.Any())
        {
            return new List<string> { "ja", "zh", "en" };
        }

        return languages;
    }

    #endregion

    #region Key Generation

    /// <summary>
    /// 生成实体显示名Key
    /// </summary>
    /// <param name="entityName">实体名称（如：Product）</param>
    /// <returns>Key（如：ENTITY_PRODUCT）</returns>
    public string GenerateEntityDisplayNameKey(string entityName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        return $"ENTITY_{entityName.ToUpperInvariant()}";
    }

    /// <summary>
    /// 生成实体描述Key
    /// </summary>
    /// <param name="entityName">实体名称（如：Product）</param>
    /// <returns>Key（如：ENTITY_PRODUCT_DESC）</returns>
    public string GenerateEntityDescriptionKey(string entityName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        return $"ENTITY_{entityName.ToUpperInvariant()}_DESC";
    }

    /// <summary>
    /// 生成字段显示名Key
    /// </summary>
    /// <param name="entityName">实体名称（如：Product）</param>
    /// <param name="fieldName">字段名称（如：Price）</param>
    /// <returns>Key（如：FIELD_PRODUCT_PRICE）</returns>
    public string GenerateFieldDisplayNameKey(string entityName, string fieldName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        return $"FIELD_{entityName.ToUpperInvariant()}_{fieldName.ToUpperInvariant()}";
    }

    #endregion

    #region Save/Update

    /// <summary>
    /// 保存或更新元数据多语言资源（动态语言支持）
    /// </summary>
    /// <param name="key">资源Key</param>
    /// <param name="translations">语言代码和翻译文本的字典</param>
    /// <returns>是否成功</returns>
    public async Task<bool> SaveOrUpdateMetadataI18nAsync(
        string key,
        Dictionary<string, string?> translations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(translations);

        if (!translations.Any())
        {
            _logger.LogWarning("[MetadataI18n] No translations provided for key: {Key}", key);
            return false;
        }

        try
        {
            // 获取现有的所有语言版本
            var existingValues = await _db.MetadataLocalizationValues
                .Where(v => v.Key == key)
                .ToListAsync();

            foreach (var (lang, text) in translations)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    // 如果文本为空，删除该语言版本
                    var toRemove = existingValues.FirstOrDefault(v =>
                        v.Language.Equals(lang, StringComparison.OrdinalIgnoreCase));
                    if (toRemove != null)
                    {
                        _db.MetadataLocalizationValues.Remove(toRemove);
                    }
                    continue;
                }

                var existing = existingValues.FirstOrDefault(v =>
                    v.Language.Equals(lang, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    // 更新现有资源
                    existing.Value = text;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // 创建新资源
                    _db.MetadataLocalizationValues.Add(new MetadataLocalizationValue
                    {
                        Key = key,
                        Language = lang.ToLowerInvariant(),
                        Value = text
                    });
                }
            }

            await _db.SaveChangesAsync();

            // 清除缓存，强制重新加载
            _localization.InvalidateCache();

            _logger.LogInformation("[MetadataI18n] Saved {Count} translations for key: {Key}",
                translations.Count, key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MetadataI18n] Failed to save resource: {Key}", key);
            return false;
        }
    }

    #endregion

    #region Delete

    /// <summary>
    /// 删除元数据多语言资源（所有语言版本）
    /// </summary>
    /// <param name="key">资源Key</param>
    /// <returns>删除的记录数</returns>
    public async Task<int> DeleteMetadataI18nAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var values = await _db.MetadataLocalizationValues
                .Where(v => v.Key == key)
                .ToListAsync();

            if (values.Any())
            {
                _db.MetadataLocalizationValues.RemoveRange(values);
                await _db.SaveChangesAsync();
                _localization.InvalidateCache();

                _logger.LogInformation("[MetadataI18n] Deleted {Count} translations for key: {Key}",
                    values.Count, key);
                return values.Count;
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MetadataI18n] Failed to delete resource: {Key}", key);
            return 0;
        }
    }

    /// <summary>
    /// 删除实体相关的所有多语言资源（包括实体本身和所有字段）
    /// </summary>
    /// <param name="entityName">实体名称</param>
    /// <param name="fieldNames">字段名称列表（可选）</param>
    /// <returns>删除的资源数量</returns>
    public async Task<int> DeleteEntityRelatedI18nAsync(
        string entityName,
        IEnumerable<string>? fieldNames = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        try
        {
            var keysToDelete = new List<string>
            {
                GenerateEntityDisplayNameKey(entityName),
                GenerateEntityDescriptionKey(entityName)
            };

            if (fieldNames != null)
            {
                foreach (var fieldName in fieldNames)
                {
                    keysToDelete.Add(GenerateFieldDisplayNameKey(entityName, fieldName));
                }
            }

            var values = await _db.MetadataLocalizationValues
                .Where(v => keysToDelete.Contains(v.Key))
                .ToListAsync();

            if (values.Any())
            {
                _db.MetadataLocalizationValues.RemoveRange(values);
                await _db.SaveChangesAsync();
                _localization.InvalidateCache();

                _logger.LogInformation(
                    "[MetadataI18n] Deleted {Count} translations for entity: {EntityName}",
                    values.Count,
                    entityName);

                return values.Count;
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MetadataI18n] Failed to delete entity related resources: {EntityName}",
                entityName);
            return 0;
        }
    }

    #endregion

    #region Query

    /// <summary>
    /// 获取元数据多语言资源
    /// </summary>
    /// <param name="key">资源Key</param>
    /// <returns>语言代码和翻译文本的字典</returns>
    public async Task<Dictionary<string, string>?> GetMetadataI18nAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var values = await _db.MetadataLocalizationValues
            .AsNoTracking()
            .Where(v => v.Key == key)
            .ToDictionaryAsync(v => v.Language, v => v.Value);

        return values.Any() ? values : null;
    }

    /// <summary>
    /// 检查资源Key是否已存在
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return await _db.MetadataLocalizationValues.AnyAsync(v => v.Key == key);
    }

    #endregion
}
