namespace BobCrm.Api.Contracts.Responses.Access;

/// <summary>
/// 用户角色分配信息。
/// </summary>
public class RoleAssignmentDto
{
    public Guid Id { get; set; }
    public Guid RoleId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

