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
    /// <summary>
    /// 同步所有国际化资源到数据库
    /// </summary>
    public async Task SyncResourcesAsync()
    {
        // 从JSON文件加载所有资源（单一数据源）
        var allResources = await I18nResourceLoader.LoadResourcesAsync();
        _logger.LogInformation("Loaded {Count} resources from JSON", allResources.Count);

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

        // Batch insert missing resources
        if (missingResources.Any())
        {
            _logger.LogInformation("Syncing {Count} missing resources...", missingResources.Count);
            var batchSize = 100;
            for (int i = 0; i < missingResources.Count; i += batchSize)
            {
                var batch = missingResources.Skip(i).Take(batchSize).ToList();
                await _db.LocalizationResources.AddRangeAsync(batch);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Inserted batch {BatchIndex}/{TotalBatches}", (i / batchSize) + 1, (int)Math.Ceiling(missingResources.Count / (double)batchSize));
            }
        }

        // Batch update existing resources
        if (updatedResources.Any())
        {
            _logger.LogInformation("Updating {Count} existing resources...", updatedResources.Count);
            var batchSize = 100;
            for (int i = 0; i < updatedResources.Count; i += batchSize)
            {
                // EF Core tracks changes, so we just need to call SaveChangesAsync
                // But we can explicitly UpdateRange if we want, though it's not strictly necessary for tracked entities.
                // To be safe and explicit:
                var batch = updatedResources.Skip(i).Take(batchSize).ToList();
                _db.LocalizationResources.UpdateRange(batch);
                await _db.SaveChangesAsync();
                _logger.LogInformation("Updated batch {BatchIndex}/{TotalBatches}", (i / batchSize) + 1, (int)Math.Ceiling(updatedResources.Count / (double)batchSize));
            }
        }

        if (!missingResources.Any() && !updatedResources.Any())
        {
            _logger.LogDebug("All i18n resources are up to date.");
        }
        else
        {
            _logger.LogInformation("I18n sync completed.");
        }
    }
}
