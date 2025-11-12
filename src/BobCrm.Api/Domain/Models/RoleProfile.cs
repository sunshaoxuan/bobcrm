using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Domain.Models;

/// <summary>
/// 角色档案 - 绑定组织并聚合功能/数据范围
/// </summary>
public class RoleProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 所属组织（为空表示全局角色）
    /// </summary>
    public Guid? OrganizationId { get; set; }

    [Required, MaxLength(64)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(256)]
    public string? Description { get; set; }

    public bool IsSystem { get; set; }
    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<RoleFunctionPermission> Functions { get; set; } = new();
    public List<RoleDataScope> DataScopes { get; set; } = new();
    public List<RoleAssignment> Assignments { get; set; } = new();
}
