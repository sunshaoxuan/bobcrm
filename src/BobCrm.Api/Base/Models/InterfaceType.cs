namespace BobCrm.Api.Base.Models;

/// <summary>
/// 接口类型枚举
/// </summary>
public static class InterfaceType
{
    /// <summary>
    /// 基础信息接口 - 提供 Id 主键
    /// </summary>
    public const string Base = "Base";

    /// <summary>
    /// 档案信息接口 - 提供 Code、Name 字段
    /// </summary>
    public const string Archive = "Archive";

    /// <summary>
    /// 审计信息接口 - 提供 CreatedAt、CreatedBy、UpdatedAt、UpdatedBy 字段
    /// </summary>
    public const string Audit = "Audit";

    /// <summary>
    /// 版本管理接口 - 提供 Version 字段（乐观锁）
    /// </summary>
    public const string Version = "Version";

    /// <summary>
    /// 时间版本接口 - 提供 ValidFrom、ValidTo、VersionNo 字段
    /// </summary>
    public const string TimeVersion = "TimeVersion";

    /// <summary>
    /// 组织维度接口 - 提供 OrganizationId/Code/Path 等字段
    /// </summary>
    public const string Organization = "Organization";
}
