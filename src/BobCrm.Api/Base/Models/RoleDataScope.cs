using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Domain.Models;

/// <summary>
/// 角色实体数据范围
/// </summary>
public class RoleDataScope
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RoleId { get; set; }
    public RoleProfile? Role { get; set; }

    [Required, MaxLength(128)]
    public string EntityName { get; set; } = string.Empty;

    [Required, MaxLength(32)]
    public string ScopeType { get; set; } = RoleDataScopeTypes.All;

    [MaxLength(512)]
    public string? FilterExpression { get; set; }
}

public static class RoleDataScopeTypes
{
    public const string All = "All";
    public const string Self = "Self";
    public const string Organization = "Organization";
    public const string OrganizationSubTree = "OrgSubTree";
    public const string Custom = "Custom";
}
