using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Base.Models;

/// <summary>
/// 用户角色分配
/// </summary>
public class RoleAssignment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string UserId { get; set; } = string.Empty;

    public Guid RoleId { get; set; }
    public RoleProfile? Role { get; set; }

    public Guid? OrganizationId { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}
