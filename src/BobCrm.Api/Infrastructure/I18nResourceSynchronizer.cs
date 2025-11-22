using BobCrm.Api.Base;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Infrastructure;

/// <summary>
/// 国际化资源同步器 - 确保数据库包含所有必需的国际化键
/// 使用 I18nResourceLoader 作为单一数据源
/// </summary>
public class I18nResourceSynchronizer
{
    private readonly AppDbContext _db;
    private readonly ILogger<I18nResourceSynchronizer> _logger;

    public I18nResourceSynchronizer(AppDbContext db, ILogger<I18nResourceSynchronizer> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// 同步所有国际化资源到数据库
    /// </summary>
    public async Task SyncResourcesAsync()
    {
        // 从JSON文件加载所有资源（单一数据源）
        var allResources = await I18nResourceLoader.LoadResourcesAsync();

        var existingKeysList = await _db.LocalizationResources
            .Select(r => r.Key)
            .ToListAsync();
        var existingKeys = existingKeysList.ToHashSet();

        var missingResources = new List<LocalizationResource>();
        var updatedResources = new List<LocalizationResource>();

        foreach (var resource in allResources)
        {
            if (!existingKeys.Contains(resource.Key))
            {
                missingResources.Add(resource);
            }
            else
            {
                // Check if updates are needed
                var existing = await _db.LocalizationResources.FirstOrDefaultAsync(r => r.Key == resource.Key);
                if (existing != null)
                {
                    bool changed = false;
                    foreach (var kvp in resource.Translations)
                    {
                        if (!existing.Translations.TryGetValue(kvp.Key, out var existingVal) || existingVal != kvp.Value)
                        {
                            existing.Translations[kvp.Key] = kvp.Value;
                            changed = true;
                        }
                    }
                    
                    if (changed)
                    {
                        updatedResources.Add(existing);
                    }
                }
            }
        }

        if (missingResources.Any())
        {
            _logger.LogInformation("同步 {Count} 个缺失的国际化资源到数据库", missingResources.Count);
            await _db.LocalizationResources.AddRangeAsync(missingResources);
        }

        if (updatedResources.Any())
        {
             _logger.LogInformation("更新 {Count} 个已存在的国际化资源", updatedResources.Count);
             _db.LocalizationResources.UpdateRange(updatedResources);
        }

        if (missingResources.Any() || updatedResources.Any())
        {
            await _db.SaveChangesAsync();
            _logger.LogInformation("成功同步国际化资源");
        }
        else
        {
            _logger.LogDebug("所有国际化资源已最新，无需同步");
        }
    }
}
