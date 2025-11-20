using System.ComponentModel.DataAnnotations;
using BobCrm.Api.Abstractions;

namespace BobCrm.Api.Base.Models;

/// <summary>
/// 字段级权限 - 控制角色对特定实体字段的读写访问权限
/// </summary>
public class FieldPermission : IAuditableEntity
{
    public int Id { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    [Required]
    public Guid RoleId { get; set; }

    /// <summary>
    /// 实体类型（如 "Customer", "Order"）
    /// </summary>
    [Required, MaxLength(128)]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 字段名（如 "Salary", "Email"）
    /// </summary>
    [Required, MaxLength(128)]
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// 可读权限
    /// </summary>
    public bool CanRead { get; set; } = true;

    /// <summary>
    /// 可写权限
    /// </summary>
    public bool CanWrite { get; set; } = false;

    /// <summary>
    /// 备注说明
    /// </summary>
    [MaxLength(512)]
    public string? Remarks { get; set; }

    // IAuditableEntity 实现
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }

    // 导航属性
    public RoleProfile? Role { get; set; }
}
