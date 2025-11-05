using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

/// <summary>
/// 实体元数据服务 - 从数据库动态加载可用于创建模板的根实体
/// 原则：只有根实体（非聚合子实体）才能创建独立模板
/// </summary>
public class EntityMetadataService
{
    private readonly AppDbContext _db;

    public EntityMetadataService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 获取所有可用的根实体（已启用且为根实体）
    /// </summary>
    public async Task<List<Data.Entities.EntityMetadata>> GetAvailableRootEntitiesAsync()
    {
        return await _db.EntityMetadata
            .Where(e => e.IsRootEntity && e.IsEnabled)
            .OrderBy(e => e.Order)
            .ToListAsync();
    }

    /// <summary>
    /// 获取所有根实体（包括未启用的）
    /// </summary>
    public async Task<List<Data.Entities.EntityMetadata>> GetAllRootEntitiesAsync()
    {
        return await _db.EntityMetadata
            .Where(e => e.IsRootEntity)
            .OrderBy(e => e.Order)
            .ToListAsync();
    }

    /// <summary>
    /// 根据类型获取实体元数据
    /// </summary>
    public async Task<Data.Entities.EntityMetadata?> GetEntityMetadataAsync(string entityType)
    {
        return await _db.EntityMetadata
            .FirstOrDefaultAsync(e => e.EntityType == entityType.ToLowerInvariant());
    }

    /// <summary>
    /// 验证实体类型是否可用于创建模板
    /// </summary>
    public async Task<bool> IsValidEntityTypeAsync(string entityType)
    {
        var entity = await GetEntityMetadataAsync(entityType);
        return entity != null && entity.IsRootEntity && entity.IsEnabled;
    }

    /// <summary>
    /// 添加新的实体元数据（管理员功能）
    /// </summary>
    public async Task<Data.Entities.EntityMetadata> AddEntityAsync(Data.Entities.EntityMetadata entity)
    {
        entity.EntityType = entity.EntityType.ToLowerInvariant();
        entity.CreatedAt = DateTime.UtcNow;
        
        _db.EntityMetadata.Add(entity);
        await _db.SaveChangesAsync();
        
        return entity;
    }

    /// <summary>
    /// 更新实体元数据（管理员功能）
    /// </summary>
    public async Task<bool> UpdateEntityAsync(string entityType, Data.Entities.EntityMetadata updatedEntity)
    {
        var existing = await GetEntityMetadataAsync(entityType);
        if (existing == null) return false;

        existing.DisplayNameKey = updatedEntity.DisplayNameKey;
        existing.DescriptionKey = updatedEntity.DescriptionKey;
        existing.ApiEndpoint = updatedEntity.ApiEndpoint;
        existing.IsRootEntity = updatedEntity.IsRootEntity;
        existing.IsEnabled = updatedEntity.IsEnabled;
        existing.Order = updatedEntity.Order;
        existing.Icon = updatedEntity.Icon;
        existing.Category = updatedEntity.Category;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }
}
