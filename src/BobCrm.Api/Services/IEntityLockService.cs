namespace BobCrm.Api.Services;

/// <summary>
/// 实体锁定服务接口
/// 管理实体定义的锁定状态，防止已被使用的实体被误修改
/// </summary>
public interface IEntityLockService
{
    /// <summary>
    /// 锁定实体定义
    /// </summary>
    /// <param name="entityId">实体ID</param>
    /// <param name="reason">锁定原因</param>
    /// <returns>是否锁定成功</returns>
    Task<bool> LockEntityAsync(Guid entityId, string reason);

    /// <summary>
    /// 批量锁定实体（如锁定整个主子表结构）
    /// </summary>
    /// <param name="rootEntityId">根实体ID</param>
    /// <param name="reason">锁定原因</param>
    /// <returns>锁定的实体数量</returns>
    Task<int> LockEntityHierarchyAsync(Guid rootEntityId, string reason);

    /// <summary>
    /// 解锁实体定义（仅管理员）
    /// </summary>
    /// <param name="entityId">实体ID</param>
    /// <param name="reason">解锁原因</param>
    /// <returns>是否解锁成功</returns>
    Task<bool> UnlockEntityAsync(Guid entityId, string reason);

    /// <summary>
    /// 检查实体是否已锁定
    /// </summary>
    /// <param name="entityId">实体ID</param>
    /// <returns>是否已锁定</returns>
    Task<bool> IsEntityLockedAsync(Guid entityId);

    /// <summary>
    /// 检查是否可以修改实体的关键属性
    /// </summary>
    /// <param name="entityId">实体ID</param>
    /// <param name="propertyName">属性名</param>
    /// <returns>是否允许修改及原因</returns>
    Task<(bool Allowed, string? Reason)> CanModifyPropertyAsync(Guid entityId, string propertyName);

    /// <summary>
    /// 获取锁定实体的引用信息
    /// </summary>
    /// <param name="entityId">实体ID</param>
    /// <returns>锁定信息</returns>
    Task<EntityLockInfo> GetLockInfoAsync(Guid entityId);

    /// <summary>
    /// 验证修改请求是否被锁定限制
    /// </summary>
    /// <param name="entityId">实体ID</param>
    /// <param name="updateRequest">更新请求</param>
    /// <returns>验证结果</returns>
    Task<ValidationResult> ValidateModificationAsync(
        Guid entityId,
        EntityDefinitionUpdateRequest updateRequest);
}
