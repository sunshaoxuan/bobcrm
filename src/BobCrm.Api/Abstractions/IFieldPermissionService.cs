using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Abstractions;

/// <summary>
/// 字段级权限服务接口
/// </summary>
public interface IFieldPermissionService
{
    /// <summary>
    /// 获取角色的所有字段权限
    /// </summary>
    Task<List<FieldPermission>> GetPermissionsByRoleAsync(Guid roleId);

    /// <summary>
    /// 获取角色对特定实体的字段权限
    /// </summary>
    Task<List<FieldPermission>> GetPermissionsByRoleAndEntityAsync(Guid roleId, string entityType);

    /// <summary>
    /// 获取用户对特定实体字段的权限（聚合所有角色）
    /// </summary>
    Task<FieldPermission?> GetUserFieldPermissionAsync(string userId, string entityType, string fieldName);

    /// <summary>
    /// 创建或更新字段权限
    /// </summary>
    Task<FieldPermission> UpsertPermissionAsync(Guid roleId, string entityType, string fieldName, bool canRead, bool canWrite, string? remarks = null, string? userId = null);

    /// <summary>
    /// 批量设置角色的字段权限
    /// </summary>
    Task BulkUpsertPermissionsAsync(Guid roleId, string entityType, List<FieldPermissionDto> permissions, string? userId = null);

    /// <summary>
    /// 删除字段权限
    /// </summary>
    Task DeletePermissionAsync(int permissionId);

    /// <summary>
    /// 删除角色的所有字段权限
    /// </summary>
    Task DeletePermissionsByRoleAsync(Guid roleId);

    /// <summary>
    /// 检查用户是否可以读取字段
    /// </summary>
    Task<bool> CanUserReadFieldAsync(string userId, string entityType, string fieldName);

    /// <summary>
    /// 检查用户是否可以写入字段
    /// </summary>
    Task<bool> CanUserWriteFieldAsync(string userId, string entityType, string fieldName);

    /// <summary>
    /// 获取用户对实体的所有可读字段列表
    /// </summary>
    Task<List<string>> GetReadableFieldsAsync(string userId, string entityType);

    /// <summary>
    /// 获取用户对实体的所有可写字段列表
    /// </summary>
    Task<List<string>> GetWritableFieldsAsync(string userId, string entityType);
}
