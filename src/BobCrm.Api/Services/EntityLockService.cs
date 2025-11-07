using BobCrm.Api.Data;
using BobCrm.Api.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

/// <summary>
/// 实体锁定服务
/// 管理实体定义的锁定状态，防止已被使用的实体被误修改
/// </summary>
public class EntityLockService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EntityLockService> _logger;

    public EntityLockService(
        ApplicationDbContext context,
        ILogger<EntityLockService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 锁定实体定义
    /// </summary>
    /// <param name="entityId">实体ID</param>
    /// <param name="reason">锁定原因</param>
    public async Task<bool> LockEntityAsync(Guid entityId, string reason)
    {
        var entity = await _context.EntityDefinitions.FindAsync(entityId);
        if (entity == null)
        {
            _logger.LogWarning("[EntityLock] Entity {EntityId} not found", entityId);
            return false;
        }

        if (entity.IsLocked)
        {
            _logger.LogInformation("[EntityLock] Entity {EntityName} is already locked", entity.EntityName);
            return true;
        }

        entity.IsLocked = true;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "[EntityLock] Locked entity {EntityName} ({EntityId}). Reason: {Reason}",
            entity.EntityName,
            entityId,
            reason);

        return true;
    }

    /// <summary>
    /// 批量锁定实体（如锁定整个主子表结构）
    /// </summary>
    public async Task<int> LockEntityHierarchyAsync(Guid rootEntityId, string reason)
    {
        var lockedCount = 0;

        // 锁定根实体
        if (await LockEntityAsync(rootEntityId, reason))
        {
            lockedCount++;
        }

        // 查找并锁定所有子实体
        var childEntities = await _context.EntityDefinitions
            .Where(e => e.ParentEntityId == rootEntityId)
            .ToListAsync();

        foreach (var child in childEntities)
        {
            if (await LockEntityAsync(child.Id, $"{reason} (child of {rootEntityId})"))
            {
                lockedCount++;
            }

            // 递归锁定孙实体
            var grandchildCount = await LockEntityHierarchyAsync(child.Id, reason);
            lockedCount += grandchildCount;
        }

        return lockedCount;
    }

    /// <summary>
    /// 解锁实体定义（仅管理员）
    /// </summary>
    public async Task<bool> UnlockEntityAsync(Guid entityId, string reason)
    {
        var entity = await _context.EntityDefinitions.FindAsync(entityId);
        if (entity == null)
        {
            _logger.LogWarning("[EntityLock] Entity {EntityId} not found", entityId);
            return false;
        }

        if (!entity.IsLocked)
        {
            _logger.LogInformation("[EntityLock] Entity {EntityName} is not locked", entity.EntityName);
            return true;
        }

        entity.IsLocked = false;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "[EntityLock] Unlocked entity {EntityName} ({EntityId}). Reason: {Reason}",
            entity.EntityName,
            entityId,
            reason);

        return true;
    }

    /// <summary>
    /// 检查实体是否已锁定
    /// </summary>
    public async Task<bool> IsEntityLockedAsync(Guid entityId)
    {
        var entity = await _context.EntityDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == entityId);

        return entity?.IsLocked ?? false;
    }

    /// <summary>
    /// 检查是否可以修改实体的关键属性
    /// </summary>
    /// <param name="entityId">实体ID</param>
    /// <param name="propertyName">属性名</param>
    /// <returns>是否允许修改</returns>
    public async Task<(bool Allowed, string? Reason)> CanModifyPropertyAsync(Guid entityId, string propertyName)
    {
        var isLocked = await IsEntityLockedAsync(entityId);

        if (!isLocked)
        {
            return (true, null);
        }

        // 锁定后不可修改的关键属性
        var restrictedProperties = new[]
        {
            nameof(EntityDefinition.Namespace),
            nameof(EntityDefinition.EntityName),
            nameof(EntityDefinition.FullTypeName),
            nameof(EntityDefinition.StructureType),
            "Interfaces" // 接口配置
        };

        if (restrictedProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase))
        {
            return (false, $"实体已被锁定，不允许修改 {propertyName} 属性");
        }

        return (true, null);
    }

    /// <summary>
    /// 获取锁定实体的引用信息
    /// </summary>
    public async Task<EntityLockInfo> GetLockInfoAsync(Guid entityId)
    {
        var entity = await _context.EntityDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == entityId);

        if (entity == null)
        {
            throw new ArgumentException($"Entity {entityId} not found");
        }

        var lockInfo = new EntityLockInfo
        {
            EntityId = entityId,
            EntityName = entity.EntityName,
            IsLocked = entity.IsLocked,
            Reasons = new List<string>()
        };

        if (!entity.IsLocked)
        {
            return lockInfo;
        }

        // 检查锁定原因

        // 1. 是否已发布
        if (entity.Status == EntityStatus.Published)
        {
            lockInfo.Reasons.Add("实体已发布");
        }

        // 2. 是否被模板引用
        var templateCount = await _context.FormTemplates
            .CountAsync(t => t.EntityType == entity.FullTypeName);
        if (templateCount > 0)
        {
            lockInfo.Reasons.Add($"被 {templateCount} 个表单模板引用");
        }

        // 3. 是否有子实体
        var childCount = await _context.EntityDefinitions
            .CountAsync(e => e.ParentEntityId == entityId);
        if (childCount > 0)
        {
            lockInfo.Reasons.Add($"作为 {childCount} 个子实体的父实体");
        }

        // 4. 是否被其他实体引用（外键）
        var referencedByCount = await _context.FieldMetadatas
            .CountAsync(f => f.ReferencedEntityId == entityId);
        if (referencedByCount > 0)
        {
            lockInfo.Reasons.Add($"被 {referencedByCount} 个字段引用");
        }

        return lockInfo;
    }

    /// <summary>
    /// 验证修改请求是否被锁定限制
    /// </summary>
    public async Task<ValidationResult> ValidateModificationAsync(
        Guid entityId,
        EntityDefinitionUpdateRequest updateRequest)
    {
        var result = new ValidationResult { IsValid = true };

        var entity = await _context.EntityDefinitions.FindAsync(entityId);
        if (entity == null)
        {
            result.IsValid = false;
            result.Errors.Add("实体不存在");
            return result;
        }

        if (!entity.IsLocked)
        {
            return result; // 未锁定，允许所有修改
        }

        // 检查是否修改了受限属性
        if (updateRequest.EntityName != null && updateRequest.EntityName != entity.EntityName)
        {
            result.IsValid = false;
            result.Errors.Add("实体已锁定，不允许修改实体名称");
        }

        if (updateRequest.Namespace != null && updateRequest.Namespace != entity.Namespace)
        {
            result.IsValid = false;
            result.Errors.Add("实体已锁定，不允许修改命名空间");
        }

        if (updateRequest.StructureType != null && updateRequest.StructureType != entity.StructureType)
        {
            result.IsValid = false;
            result.Errors.Add("实体已锁定，不允许修改结构类型");
        }

        return result;
    }
}

/// <summary>
/// 实体锁定信息
/// </summary>
public class EntityLockInfo
{
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = "";
    public bool IsLocked { get; set; }
    public List<string> Reasons { get; set; } = new();
}

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// 实体定义更新请求（用于验证）
/// </summary>
public class EntityDefinitionUpdateRequest
{
    public string? EntityName { get; set; }
    public string? Namespace { get; set; }
    public string? StructureType { get; set; }
    public string? DisplayNameKey { get; set; }
    public string? DescriptionKey { get; set; }
    public string? Icon { get; set; }
    public string? Category { get; set; }
    public int? Order { get; set; }
    public bool? IsEnabled { get; set; }
}
