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

        var missingResources = allResources
            .Where(r => !existingKeys.Contains(r.Key))
            .ToList();

        if (missingResources.Any())
        {
            _logger.LogInformation("同步 {Count} 个缺失的国际化资源到数据库", missingResources.Count);
            await _db.LocalizationResources.AddRangeAsync(missingResources);
            await _db.SaveChangesAsync();
            _logger.LogInformation("成功同步国际化资源");
        }
        else
        {
            _logger.LogDebug("所有国际化资源已存在，无需同步");
        }
    }
}
