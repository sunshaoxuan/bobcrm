using BobCrm.Api.Abstractions;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Core.DomainCommon;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BobCrm.Api.Services;

/// <summary>
/// 字段级权限服务实现
/// </summary>
public class FieldPermissionService : IFieldPermissionService
{
    private readonly IRepository<FieldPermission> _repo;
    private readonly AppDbContext _dbContext;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<FieldPermissionService> _logger;

    public FieldPermissionService(
        IRepository<FieldPermission> repo,
        AppDbContext dbContext,
        IUnitOfWork uow,
        ILogger<FieldPermissionService> logger)
    {
        _repo = repo;
        _dbContext = dbContext;
        _uow = uow;
        _logger = logger;
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
        // 获取用户的所有角色ID
        var roleIds = await _dbContext.RoleAssignments
            .Where(ra => ra.UserId == userId &&
                         (!ra.ValidFrom.HasValue || ra.ValidFrom <= DateTime.UtcNow) &&
                         (!ra.ValidTo.HasValue || ra.ValidTo >= DateTime.UtcNow))
            .Select(ra => ra.RoleId)
            .Distinct()
            .ToListAsync();

        if (!roleIds.Any())
        {
            return null;
        }

        // 获取用户所有角色对该字段的权限（取最宽松的权限）
        var permissions = await _dbContext.FieldPermissions
            .Where(fp => roleIds.Contains(fp.RoleId) &&
                        fp.EntityType == entityType &&
                        fp.FieldName == fieldName)
            .ToListAsync();

        if (!permissions.Any())
        {
            // 如果没有显式权限，默认为可读不可写
            return new FieldPermission
            {
                EntityType = entityType,
                FieldName = fieldName,
                CanRead = true,
                CanWrite = false
            };
        }

        // 聚合权限：只要有一个角色允许，就允许
        return new FieldPermission
        {
            EntityType = entityType,
            FieldName = fieldName,
            CanRead = permissions.Any(p => p.CanRead),
            CanWrite = permissions.Any(p => p.CanWrite)
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

        _repo.Remove(permission);
        await _uow.SaveChangesAsync();

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
        // 获取用户的所有角色ID
        var roleIds = await _dbContext.RoleAssignments
            .Where(ra => ra.UserId == userId &&
                         (!ra.ValidFrom.HasValue || ra.ValidFrom <= DateTime.UtcNow) &&
                         (!ra.ValidTo.HasValue || ra.ValidTo >= DateTime.UtcNow))
            .Select(ra => ra.RoleId)
            .Distinct()
            .ToListAsync();

        if (!roleIds.Any())
        {
            // 如果用户没有角色，返回空列表（严格模式）或所有字段（宽松模式）
            return new List<string>();
        }

        // 获取所有角色对该实体的字段权限
        var permissions = await _dbContext.FieldPermissions
            .Where(fp => roleIds.Contains(fp.RoleId) && fp.EntityType == entityType && fp.CanRead)
            .Select(fp => fp.FieldName)
            .Distinct()
            .ToListAsync();

        return permissions;
    }

    public async Task<List<string>> GetWritableFieldsAsync(string userId, string entityType)
    {
        // 获取用户的所有角色ID
        var roleIds = await _dbContext.RoleAssignments
            .Where(ra => ra.UserId == userId &&
                         (!ra.ValidFrom.HasValue || ra.ValidFrom <= DateTime.UtcNow) &&
                         (!ra.ValidTo.HasValue || ra.ValidTo >= DateTime.UtcNow))
            .Select(ra => ra.RoleId)
            .Distinct()
            .ToListAsync();

        if (!roleIds.Any())
        {
            return new List<string>();
        }

        // 获取所有角色对该实体的字段权限
        var permissions = await _dbContext.FieldPermissions
            .Where(fp => roleIds.Contains(fp.RoleId) && fp.EntityType == entityType && fp.CanWrite)
            .Select(fp => fp.FieldName)
            .Distinct()
            .ToListAsync();

        return permissions;
    }
}
