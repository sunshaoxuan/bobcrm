using System.Reflection;
using Microsoft.EntityFrameworkCore;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Domain.Models;

namespace BobCrm.Api.Infrastructure;

/// <summary>
/// 实体定义同步器 - 在系统启动时自动同步系统实体到EntityDefinition表
/// </summary>
public class EntityDefinitionSynchronizer
{
    private readonly AppDbContext _db;
    private readonly ILogger<EntityDefinitionSynchronizer> _logger;

    public EntityDefinitionSynchronizer(AppDbContext db, ILogger<EntityDefinitionSynchronizer> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// 同步所有实现IBizEntity的系统实体
    /// </summary>
    public async Task SyncSystemEntitiesAsync()
    {
        _logger.LogInformation("[EntitySync] ========== Starting system entity synchronization ==========");

        try
        {
            // 扫描所有实现IBizEntity的类
            var bizEntityTypes = ScanBizEntityTypes();
            _logger.LogInformation("[EntitySync] Found {Count} IBizEntity implementations", bizEntityTypes.Count);

            var syncedCount = 0;
            var skippedCount = 0;

            foreach (var entityType in bizEntityTypes)
            {
                try
                {
                    var result = await SyncEntityAsync(entityType);
                    if (result)
                        syncedCount++;
                    else
                        skippedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[EntitySync] Failed to sync entity: {TypeName}", entityType.Name);
                }
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("[EntitySync] ========== Synchronization completed: {Synced} synced, {Skipped} skipped ==========",
                syncedCount, skippedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EntitySync] Synchronization failed with error");
            throw;
        }
    }

    /// <summary>
    /// 扫描程序集中所有实现IBizEntity的类型
    /// </summary>
    private List<Type> ScanBizEntityTypes()
    {
        var assembly = typeof(Domain.Customer).Assembly;
        var bizEntityInterface = typeof(IBizEntity);

        var types = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetInterfaces().Contains(bizEntityInterface))
            .ToList();

        return types;
    }

    /// <summary>
    /// 同步单个实体
    /// </summary>
    /// <returns>是否进行了同步（true=新增或更新，false=已存在跳过）</returns>
    private async Task<bool> SyncEntityAsync(Type entityType)
    {
        // 通过反射调用静态方法 GetInitialDefinition()
        var getDefMethod = entityType.GetMethod(
            "GetInitialDefinition",
            BindingFlags.Public | BindingFlags.Static);

        if (getDefMethod == null)
        {
            _logger.LogWarning("[EntitySync] {TypeName} missing GetInitialDefinition method, skipping",
                entityType.Name);
            return false;
        }

        var definition = (EntityDefinition?)getDefMethod.Invoke(null, null);
        if (definition == null)
        {
            _logger.LogWarning("[EntitySync] {TypeName}.GetInitialDefinition() returned null, skipping",
                entityType.Name);
            return false;
        }

        // 检查数据库中是否已存在
        var existing = await _db.EntityDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(ed => ed.FullTypeName == definition.FullTypeName);

        if (existing == null)
        {
            // 首次启动，插入新实体定义
            _logger.LogInformation("[EntitySync] Inserting new system entity: {FullTypeName}",
                definition.FullTypeName);

            // 确保Source为System
            definition.Source = EntitySource.System;
            definition.CreatedAt = DateTime.UtcNow;
            definition.UpdatedAt = DateTime.UtcNow;

            await _db.EntityDefinitions.AddAsync(definition);
            return true;
        }
        else
        {
            // 实体已存在
            _logger.LogDebug("[EntitySync] Entity already exists, skipping: {FullTypeName}",
                definition.FullTypeName);

            // 可选：检查是否需要更新非用户可修改的字段
            // 例如：DisplayNameKey、Icon等元数据字段可以自动更新
            // 但Fields、Interfaces等用户可能已修改的字段不应覆盖

            // 暂时不做任何更新，完全以数据库为准
            return false;
        }
    }

    /// <summary>
    /// 重置系统实体为默认定义（可选功能，供管理员使用）
    /// </summary>
    public async Task ResetSystemEntityAsync(string fullTypeName)
    {
        _logger.LogWarning("[EntitySync] Resetting system entity to default: {FullTypeName}", fullTypeName);

        // 查找对应的类型
        var entityType = typeof(Domain.Customer).Assembly.GetTypes()
            .FirstOrDefault(t => t.FullName == fullTypeName);

        if (entityType == null)
        {
            throw new InvalidOperationException($"Entity type not found: {fullTypeName}");
        }

        // 获取初始定义
        var getDefMethod = entityType.GetMethod(
            "GetInitialDefinition",
            BindingFlags.Public | BindingFlags.Static);

        if (getDefMethod == null)
        {
            throw new InvalidOperationException($"Entity type does not have GetInitialDefinition method: {fullTypeName}");
        }

        var initialDef = (EntityDefinition?)getDefMethod.Invoke(null, null);
        if (initialDef == null)
        {
            throw new InvalidOperationException($"GetInitialDefinition returned null for: {fullTypeName}");
        }

        // 查找现有定义
        var existing = await _db.EntityDefinitions
            .Include(ed => ed.Fields)
            .Include(ed => ed.Interfaces)
            .FirstOrDefaultAsync(ed => ed.FullTypeName == fullTypeName);

        if (existing == null)
        {
            throw new InvalidOperationException($"Entity definition not found in database: {fullTypeName}");
        }

        if (existing.Source != EntitySource.System)
        {
            throw new InvalidOperationException($"Cannot reset non-system entity: {fullTypeName}");
        }

        // 删除旧的Fields和Interfaces
        _db.FieldMetadatas.RemoveRange(existing.Fields);
        _db.EntityInterfaces.RemoveRange(existing.Interfaces);

        // 更新基本属性
        existing.Namespace = initialDef.Namespace;
        existing.EntityName = initialDef.EntityName;
        existing.EntityRoute = initialDef.EntityRoute;
        existing.DisplayNameKey = initialDef.DisplayNameKey;
        existing.DescriptionKey = initialDef.DescriptionKey;
        existing.ApiEndpoint = initialDef.ApiEndpoint;
        existing.StructureType = initialDef.StructureType;
        existing.Status = initialDef.Status;
        existing.IsRootEntity = initialDef.IsRootEntity;
        existing.IsEnabled = initialDef.IsEnabled;
        existing.Order = initialDef.Order;
        existing.Icon = initialDef.Icon;
        existing.Category = initialDef.Category;
        existing.UpdatedAt = DateTime.UtcNow;

        // 添加新的Fields和Interfaces
        existing.Fields = initialDef.Fields;
        existing.Interfaces = initialDef.Interfaces;

        await _db.SaveChangesAsync();

        _logger.LogInformation("[EntitySync] System entity reset completed: {FullTypeName}", fullTypeName);
    }
}
