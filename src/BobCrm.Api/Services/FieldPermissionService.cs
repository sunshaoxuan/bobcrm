using BobCrm.Api.Abstractions;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Core.DomainCommon;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BobCrm.Api.Services;

/// <summary>
/// 字段级权限服务实现（带缓存优化）
/// </summary>
public class FieldPermissionService : IFieldPermissionService
{
    private readonly IRepository<FieldPermission> _repo;
    private readonly AppDbContext _dbContext;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FieldPermissionService> _logger;

    // 缓存配置
    private static readonly TimeSpan RoleCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan PermissionCacheDuration = TimeSpan.FromMinutes(5);

    private const string RoleCacheKeyPrefix = "UserRoles:";
    private const string PermissionCacheKeyPrefix = "FieldPerms:";

    public FieldPermissionService(
        IRepository<FieldPermission> repo,
        AppDbContext dbContext,
        IUnitOfWork uow,
        IMemoryCache cache,
        ILogger<FieldPermissionService> logger)
    {
        _repo = repo;
        _dbContext = dbContext;
        _uow = uow;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户的角色ID列表（带缓存）
    /// </summary>
    private async Task<List<Guid>> GetUserRoleIdsAsync(string userId)
    {
        var cacheKey = $"{RoleCacheKeyPrefix}{userId}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = RoleCacheDuration;
            _logger.LogDebug("[FieldPermissionService] Cache MISS for user roles: {UserId}", userId);

            var roleIds = await _dbContext.RoleAssignments
                .Where(ra => ra.UserId == userId &&
                             (!ra.ValidFrom.HasValue || ra.ValidFrom <= DateTime.UtcNow) &&
                             (!ra.ValidTo.HasValue || ra.ValidTo >= DateTime.UtcNow))
                .Select(ra => ra.RoleId)
                .Distinct()
                .ToListAsync();

            return roleIds;
        }) ?? new List<Guid>();
    }

    /// <summary>
    /// 获取用户对某实体的所有字段权限（带缓存）
    /// </summary>
    private async Task<Dictionary<string, FieldPermission>> GetUserEntityPermissionsAsync(string userId, string entityType)
    {
        var cacheKey = $"{PermissionCacheKeyPrefix}{userId}:{entityType}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = PermissionCacheDuration;
            _logger.LogDebug("[FieldPermissionService] Cache MISS for user permissions: {UserId}, {EntityType}", userId, entityType);

            var roleIds = await GetUserRoleIdsAsync(userId);

            if (!roleIds.Any())
            {
                return new Dictionary<string, FieldPermission>();
            }

            // 获取所有角色对该实体的字段权限
            var permissions = await _dbContext.FieldPermissions
                .Where(fp => roleIds.Contains(fp.RoleId) && fp.EntityType == entityType)
                .ToListAsync();

            // 按字段名聚合权限（取最宽松的权限）
            var aggregated = permissions
                .GroupBy(p => p.FieldName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => new FieldPermission
                    {
                        EntityType = entityType,
                        FieldName = g.Key,
                        CanRead = g.Any(p => p.CanRead),
                        CanWrite = g.Any(p => p.CanWrite)
                    },
                    StringComparer.OrdinalIgnoreCase);

            return aggregated;
        }) ?? new Dictionary<string, FieldPermission>();
    }

    /// <summary>
    /// 清除用户角色缓存
    /// </summary>
    private void InvalidateUserRoleCache(string userId)
    {
        var cacheKey = $"{RoleCacheKeyPrefix}{userId}";
        _cache.Remove(cacheKey);
        _logger.LogDebug("[FieldPermissionService] Invalidated role cache for user: {UserId}", userId);
    }

    /// <summary>
    /// 清除角色的所有权限缓存
    /// </summary>
    private void InvalidateRolePermissionCache(Guid roleId)
    {
        // 注意：由于缓存键包含 userId，我们需要清除所有相关用户的缓存
        // 这里使用简单的全局失效策略，实际项目中可以维护一个 roleId -> userIds 的映射
        _logger.LogWarning("[FieldPermissionService] Role {RoleId} permissions modified. Consider clearing all related user caches.", roleId);
        // TODO: 实现更精细的缓存失效策略
    }

    public async Task<List<FieldPermission>> GetPermissionsByRoleAsync(Guid roleId)
    {
        return await Task.FromResult(
            _repo.Query(fp => fp.RoleId == roleId)
                .OrderBy(fp => fp.EntityType)
                .ThenBy(fp => fp.FieldName)
                .ToList());
    }

    public async Task<List<FieldPermission>> GetPermissionsByRoleAndEntityAsync(Guid roleId, string entityType)
    {
        return await Task.FromResult(
            _repo.Query(fp => fp.RoleId == roleId && fp.EntityType == entityType)
                .OrderBy(fp => fp.FieldName)
                .ToList());
    }

    public async Task<FieldPermission?> GetUserFieldPermissionAsync(string userId, string entityType, string fieldName)
    {
        // 使用缓存获取用户对该实体的所有字段权限
        var permissions = await GetUserEntityPermissionsAsync(userId, entityType);

        // 查找特定字段的权限
        if (permissions.TryGetValue(fieldName, out var permission))
        {
            return permission;
        }

        // 如果没有显式权限，默认为可读不可写
        return new FieldPermission
        {
            EntityType = entityType,
            FieldName = fieldName,
            CanRead = true,
            CanWrite = false
        };
    }

    public async Task<FieldPermission> UpsertPermissionAsync(
        Guid roleId,
        string entityType,
        string fieldName,
        bool canRead,
        bool canWrite,
        string? remarks = null,
        string? userId = null)
    {
        // 查找现有权限
        var existing = await Task.FromResult(
            _repo.Query(fp => fp.RoleId == roleId &&
                            fp.EntityType == entityType &&
                            fp.FieldName == fieldName)
                .FirstOrDefault());

        if (existing != null)
        {
            // 更新现有权限
            existing.CanRead = canRead;
            existing.CanWrite = canWrite;
            existing.Remarks = remarks;
            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedBy = userId;

            _repo.Update(existing);
            _logger.LogInformation("[FieldPermissionService] Updated permission for role {RoleId}, entity {EntityType}, field {FieldName}",
                roleId, entityType, fieldName);
        }
        else
        {
            // 创建新权限
            existing = new FieldPermission
            {
                RoleId = roleId,
                EntityType = entityType,
                FieldName = fieldName,
                CanRead = canRead,
                CanWrite = canWrite,
                Remarks = remarks,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userId
            };

            await _repo.AddAsync(existing);
            _logger.LogInformation("[FieldPermissionService] Created permission for role {RoleId}, entity {EntityType}, field {FieldName}",
                roleId, entityType, fieldName);
        }

        await _uow.SaveChangesAsync();

        // 清除缓存
        InvalidateRolePermissionCache(roleId);

        return existing;
    }

    public async Task BulkUpsertPermissionsAsync(
        Guid roleId,
        string entityType,
        List<FieldPermissionDto> permissions,
        string? userId = null)
    {
        _logger.LogInformation("[FieldPermissionService] Bulk upserting {Count} permissions for role {RoleId}, entity {EntityType}",
            permissions.Count, roleId, entityType);

        // 获取现有权限
        var existingPermissions = await Task.FromResult(
            _repo.Query(fp => fp.RoleId == roleId && fp.EntityType == entityType)
                .ToList());

        var permissionDict = existingPermissions.ToDictionary(
            fp => fp.FieldName,
            fp => fp,
            StringComparer.OrdinalIgnoreCase);

        foreach (var dto in permissions)
        {
            if (permissionDict.TryGetValue(dto.FieldName, out var existing))
            {
                // 更新现有权限
                existing.CanRead = dto.CanRead;
                existing.CanWrite = dto.CanWrite;
                existing.Remarks = dto.Remarks;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = userId;
                _repo.Update(existing);
            }
            else
            {
                // 创建新权限
                var newPermission = new FieldPermission
                {
                    RoleId = roleId,
                    EntityType = entityType,
                    FieldName = dto.FieldName,
                    CanRead = dto.CanRead,
                    CanWrite = dto.CanWrite,
                    Remarks = dto.Remarks,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = userId
                };
                await _repo.AddAsync(newPermission);
            }
        }

        await _uow.SaveChangesAsync();

        // 清除缓存
        InvalidateRolePermissionCache(roleId);

        _logger.LogInformation("[FieldPermissionService] Bulk upsert completed for role {RoleId}, entity {EntityType}",
            roleId, entityType);
    }

    public async Task DeletePermissionAsync(int permissionId)
    {
        var permission = await Task.FromResult(_repo.Query(fp => fp.Id == permissionId).FirstOrDefault());

        if (permission == null)
        {
            throw new KeyNotFoundException($"Permission {permissionId} not found");
        }

        var roleId = permission.RoleId;
        _repo.Remove(permission);
        await _uow.SaveChangesAsync();

        // 清除缓存
        InvalidateRolePermissionCache(roleId);

        _logger.LogInformation("[FieldPermissionService] Deleted permission {PermissionId}", permissionId);
    }

    public async Task DeletePermissionsByRoleAsync(Guid roleId)
    {
        var permissions = await Task.FromResult(_repo.Query(fp => fp.RoleId == roleId).ToList());

        foreach (var permission in permissions)
        {
            _repo.Remove(permission);
        }

        await _uow.SaveChangesAsync();

        // 清除缓存
        InvalidateRolePermissionCache(roleId);

        _logger.LogInformation("[FieldPermissionService] Deleted {Count} permissions for role {RoleId}",
            permissions.Count, roleId);
    }

    public async Task<bool> CanUserReadFieldAsync(string userId, string entityType, string fieldName)
    {
        var permission = await GetUserFieldPermissionAsync(userId, entityType, fieldName);
        return permission?.CanRead ?? true; // 默认允许读取
    }

    public async Task<bool> CanUserWriteFieldAsync(string userId, string entityType, string fieldName)
    {
        var permission = await GetUserFieldPermissionAsync(userId, entityType, fieldName);
        return permission?.CanWrite ?? false; // 默认不允许写入
    }

    public async Task<List<string>> GetReadableFieldsAsync(string userId, string entityType)
    {
        // 使用缓存获取用户对该实体的所有字段权限
        var permissions = await GetUserEntityPermissionsAsync(userId, entityType);

        // 返回所有可读字段
        return permissions
            .Where(kvp => kvp.Value.CanRead)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    public async Task<List<string>> GetWritableFieldsAsync(string userId, string entityType)
    {
        // 使用缓存获取用户对该实体的所有字段权限
        var permissions = await GetUserEntityPermissionsAsync(userId, entityType);

        // 返回所有可写字段
        return permissions
            .Where(kvp => kvp.Value.CanWrite)
            .Select(kvp => kvp.Key)
            .ToList();
    }
}
