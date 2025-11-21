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
/// 蟄玲ｮｵ郤ｧ譚・剞譛榊苅螳樒鴫・亥ｸｦ郛灘ｭ倅ｼ伜喧・・/// </summary>
public class FieldPermissionService : IFieldPermissionService
{
    private readonly IRepository<FieldPermission> _repo;
    private readonly AppDbContext _dbContext;
    private readonly IUnitOfWork _uow;
    private readonly IMemoryCache _cache;
    private readonly ILogger<FieldPermissionService> _logger;

    // 郛灘ｭ倬・鄂ｮ
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
    /// 闔ｷ蜿也畑謌ｷ逧・ｧ定牡ID蛻苓｡ｨ・亥ｸｦ郛灘ｭ假ｼ・    /// </summary>
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
    /// 闔ｷ蜿也畑謌ｷ蟇ｹ譟仙ｮ樔ｽ鍋噪謇譛牙ｭ玲ｮｵ譚・剞・亥ｸｦ郛灘ｭ假ｼ・    /// </summary>
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

            // 闔ｷ蜿匁園譛芽ｧ定牡蟇ｹ隸･螳樔ｽ鍋噪蟄玲ｮｵ譚・剞
            var permissions = await _dbContext.FieldPermissions
                .Where(fp => roleIds.Contains(fp.RoleId) && fp.EntityType == entityType)
                .ToListAsync();

            // 按字段名聚合权限（取最宽松权限）
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
    /// 貂・勁逕ｨ謌ｷ隗定牡郛灘ｭ・    /// </summary>
    private void InvalidateUserRoleCache(string userId)
    {
        var cacheKey = $"{RoleCacheKeyPrefix}{userId}";
        _cache.Remove(cacheKey);
        _logger.LogDebug("[FieldPermissionService] Invalidated role cache for user: {UserId}", userId);
    }

    /// <summary>
    /// 貂・勁隗定牡逧・園譛画揀髯千ｼ灘ｭ・    /// </summary>
    private void InvalidateRolePermissionCache(Guid roleId, string? entityType = null)
    {
        var affectedUsers = _dbContext.RoleAssignments
            .Where(ra => ra.RoleId == roleId)
            .Select(ra => ra.UserId)
            .Distinct()
            .ToList();

        if (entityType is null && _cache is MemoryCache memCache)
        {
            var entries = memCache as IEnumerable<KeyValuePair<object, object?>> ?? Array.Empty<KeyValuePair<object, object?>>();
            foreach (var entry in entries)
            {
                if (entry.Key is string key &&
                    affectedUsers.Any(user => key.StartsWith($"{PermissionCacheKeyPrefix}{user}:", StringComparison.OrdinalIgnoreCase)))
                {
                    _cache.Remove(key);
                }
            }
        }

        foreach (var user in affectedUsers)
        {
            if (!string.IsNullOrEmpty(entityType))
            {
                _cache.Remove($"{PermissionCacheKeyPrefix}{user}:{entityType}");
            }
            _cache.Remove($"{RoleCacheKeyPrefix}{user}");
        }

        _logger.LogDebug("[FieldPermissionService] Cleared permission cache for role {RoleId} (affected users: {Count})", roleId, affectedUsers.Count);
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
        var permissions = await GetUserEntityPermissionsAsync(userId, entityType);

        if (permissions.TryGetValue(fieldName, out var permission))
        {
            return permission;
        }

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
        var existing = _repo.Query(fp => fp.RoleId == roleId &&
                                        fp.EntityType == entityType &&
                                        fp.FieldName == fieldName)
            .FirstOrDefault();

        if (existing != null)
        {
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
        InvalidateRolePermissionCache(roleId, entityType);

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

        var existingPermissions = _repo.Query(fp => fp.RoleId == roleId && fp.EntityType == entityType).ToList();
        var permissionDict = existingPermissions.ToDictionary(fp => fp.FieldName, fp => fp, StringComparer.OrdinalIgnoreCase);

        foreach (var dto in permissions)
        {
            if (permissionDict.TryGetValue(dto.FieldName, out var existing))
            {
                existing.CanRead = dto.CanRead;
                existing.CanWrite = dto.CanWrite;
                existing.Remarks = dto.Remarks;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = userId;
                _repo.Update(existing);
            }
            else
            {
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
        InvalidateRolePermissionCache(roleId, entityType);

        _logger.LogInformation("[FieldPermissionService] Bulk upsert completed for role {RoleId}, entity {EntityType}",
            roleId, entityType);
    }
    public async Task DeletePermissionAsync(int permissionId)
    {
        var permission = _repo.Query(fp => fp.Id == permissionId).FirstOrDefault();

        if (permission == null)
        {
            throw new KeyNotFoundException($"Permission {permissionId} not found");
        }

        var roleId = permission.RoleId;
        _repo.Remove(permission);
        await _uow.SaveChangesAsync();

        InvalidateRolePermissionCache(roleId);

        _logger.LogInformation("[FieldPermissionService] Deleted permission {PermissionId}", permissionId);
    }
    public async Task DeletePermissionsByRoleAsync(Guid roleId)
    {
        var permissions = _repo.Query(fp => fp.RoleId == roleId).ToList();

        foreach (var permission in permissions)
        {
            _repo.Remove(permission);
        }

        await _uow.SaveChangesAsync();
        InvalidateRolePermissionCache(roleId);

        _logger.LogInformation("[FieldPermissionService] Deleted {Count} permissions for role {RoleId}",
            permissions.Count, roleId);
    }
    public async Task<bool> CanUserReadFieldAsync(string userId, string entityType, string fieldName)
    {
        var permission = await GetUserFieldPermissionAsync(userId, entityType, fieldName);
        return permission?.CanRead ?? true;
    }

    public async Task<bool> CanUserWriteFieldAsync(string userId, string entityType, string fieldName)
    {
        var permission = await GetUserFieldPermissionAsync(userId, entityType, fieldName);
        return permission?.CanWrite ?? false;
    }

    public async Task<List<string>> GetReadableFieldsAsync(string userId, string entityType)
    {
        // 菴ｿ逕ｨ郛灘ｭ倩執蜿也畑謌ｷ蟇ｹ隸･螳樔ｽ鍋噪謇譛牙ｭ玲ｮｵ譚・剞
        var permissions = await GetUserEntityPermissionsAsync(userId, entityType);

        // 霑泌屓謇譛牙庄隸ｻ蟄玲ｮｵ
        return permissions
            .Where(kvp => kvp.Value.CanRead)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    public async Task<List<string>> GetWritableFieldsAsync(string userId, string entityType)
    {
        // 菴ｿ逕ｨ郛灘ｭ倩執蜿也畑謌ｷ蟇ｹ隸･螳樔ｽ鍋噪謇譛牙ｭ玲ｮｵ譚・剞
        var permissions = await GetUserEntityPermissionsAsync(userId, entityType);

        // 霑泌屓謇譛牙庄蜀吝ｭ玲ｮｵ
        return permissions
            .Where(kvp => kvp.Value.CanWrite)
            .Select(kvp => kvp.Key)
            .ToList();
    }
}











