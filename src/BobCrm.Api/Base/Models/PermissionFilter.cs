using System;
using System.Collections.Generic;

namespace BobCrm.Api.Base.Models;

/// <summary>
/// 权限过滤器 - 运行态自动注入基于角色的数据访问范围
/// </summary>
public class PermissionFilter
{
    /// <summary>权限过滤器ID</summary>
    public int Id { get; set; }

    /// <summary>过滤器代码(业务标识)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>过滤器名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 过滤器显示名称(多语)
    /// </summary>
    public Dictionary<string, string?>? DisplayName { get; set; }

    /// <summary>
    /// 描述(多语)
    /// </summary>
    public Dictionary<string, string?>? Description { get; set; }

    /// <summary>
    /// 所需功能权限代码
    /// 当访问数据时,必须拥有此功能权限
    /// 例如: "CUSTOMER.VIEW", "ORG.MANAGE"
    /// </summary>
    public string? RequiredFunctionCode { get; set; }

    /// <summary>
    /// 数据范围标签
    /// 引用 RoleDataScope 的 ScopeTag
    /// 例如: "CustomerAccess", "OrganizationScope"
    /// </summary>
    public string? DataScopeTag { get; set; }

    /// <summary>
    /// 适用的实体类型(可选)
    /// 如果指定,则仅对该实体类型生效
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// 过滤规则(JSON 格式)
    /// 描述如何根据角色数据范围生成过滤条件
    ///
    /// 格式:
    /// {
    ///   "scopeType": "Organization",
    ///   "field": "organizationId",
    ///   "behavior": "Include" | "Exclude"
    /// }
    ///
    /// 当 RoleDataScope.ScopeType = All 时,不附加任何过滤
    /// 当 RoleDataScope.ScopeType = Organization 时,根据 OrganizationIds 过滤
    /// 当 RoleDataScope.ScopeType = Self 时,根据 CurrentUserId 过滤
    /// </summary>
    public string? FilterRulesJson { get; set; }

    /// <summary>
    /// 是否启用字段级权限(预留)
    /// 未来版本可支持基于角色隐藏/只读特定字段
    /// </summary>
    public bool EnableFieldLevelPermissions { get; set; } = false;

    /// <summary>
    /// 字段权限规则(JSON 格式,预留)
    /// 格式: [
    ///   {
    ///     "field": "salary",
    ///     "roles": ["HR.MANAGER"],
    ///     "access": "ReadWrite"
    ///   }, ...
    /// ]
    /// </summary>
    public string? FieldPermissionsJson { get; set; }

    /// <summary>是否为系统内置过滤器</summary>
    public bool IsSystem { get; set; } = false;

    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>创建者ID</summary>
    public string? CreatedBy { get; set; }

    /// <summary>更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>更新者ID</summary>
    public string? UpdatedBy { get; set; }
}
