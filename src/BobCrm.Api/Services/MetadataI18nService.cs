using BobCrm.Api.Domain;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

/// <summary>
/// 元数据国际化服务
/// 负责管理实体定义和字段的多语言资源
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
    /// 保存或更新元数据多语言资源
    /// </summary>
    /// <param name="key">资源Key</param>
    /// <param name="zh">中文文本</param>
    /// <param name="ja">日语文本</param>
    /// <param name="en">英文文本</param>
    /// <returns>是否成功</returns>
    public async Task<bool> SaveOrUpdateMetadataI18nAsync(
        string key,
        string? zh,
        string? ja,
        string? en)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var existing = await _db.LocalizationResources
                .FirstOrDefaultAsync(r => r.Key == key);

            if (existing != null)
            {
                // 更新现有资源
                existing.ZH = zh;
                existing.JA = ja;
                existing.EN = en;
                _logger.LogInformation("[MetadataI18n] Updated resource: {Key}", key);
            }
            else
            {
                // 创建新资源
                var resource = new LocalizationResource
                {
                    Key = key,
                    ZH = zh,
                    JA = ja,
                    EN = en
                };
                _db.LocalizationResources.Add(resource);
                _logger.LogInformation("[MetadataI18n] Created resource: {Key}", key);
            }

            await _db.SaveChangesAsync();

            // 清除缓存，强制重新加载
            _localization.InvalidateCache();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MetadataI18n] Failed to save resource: {Key}", key);
            return false;
        }
    }

    /// <summary>
    /// 批量保存或更新元数据多语言资源
    /// </summary>
    public async Task<bool> SaveOrUpdateBatchAsync(
        Dictionary<string, (string? zh, string? ja, string? en)> resources)
    {
        ArgumentNullException.ThrowIfNull(resources);

        try
        {
            foreach (var kvp in resources)
            {
                var key = kvp.Key;
                var (zh, ja, en) = kvp.Value;

                var existing = await _db.LocalizationResources
                    .FirstOrDefaultAsync(r => r.Key == key);

                if (existing != null)
                {
                    existing.ZH = zh;
                    existing.JA = ja;
                    existing.EN = en;
                }
                else
                {
                    _db.LocalizationResources.Add(new LocalizationResource
                    {
                        Key = key,
                        ZH = zh,
                        JA = ja,
                        EN = en
                    });
                }
            }

            await _db.SaveChangesAsync();
            _localization.InvalidateCache();

            _logger.LogInformation("[MetadataI18n] Batch saved {Count} resources", resources.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MetadataI18n] Failed to batch save resources");
            return false;
        }
    }

    #endregion

    #region Delete

    /// <summary>
    /// 删除元数据多语言资源
    /// </summary>
    /// <param name="key">资源Key</param>
    /// <returns>是否成功</returns>
    public async Task<bool> DeleteMetadataI18nAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var resource = await _db.LocalizationResources
                .FirstOrDefaultAsync(r => r.Key == key);

            if (resource != null)
            {
                _db.LocalizationResources.Remove(resource);
                await _db.SaveChangesAsync();
                _localization.InvalidateCache();

                _logger.LogInformation("[MetadataI18n] Deleted resource: {Key}", key);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MetadataI18n] Failed to delete resource: {Key}", key);
            return false;
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

            var resources = await _db.LocalizationResources
                .Where(r => keysToDelete.Contains(r.Key))
                .ToListAsync();

            if (resources.Any())
            {
                _db.LocalizationResources.RemoveRange(resources);
                await _db.SaveChangesAsync();
                _localization.InvalidateCache();

                _logger.LogInformation(
                    "[MetadataI18n] Deleted {Count} resources for entity: {EntityName}",
                    resources.Count,
                    entityName);

                return resources.Count;
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
    /// <returns>多语言文本（ZH, JA, EN）</returns>
    public async Task<(string? zh, string? ja, string? en)?> GetMetadataI18nAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var resource = await _db.LocalizationResources
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Key == key);

        if (resource == null)
        {
            return null;
        }

        return (resource.ZH, resource.JA, resource.EN);
    }

    /// <summary>
    /// 检查资源Key是否已存在
    /// </summary>
    public async Task<bool> ExistsAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return await _db.LocalizationResources.AnyAsync(r => r.Key == key);
    }

    #endregion
}
