namespace BobCrm.App.Models;

public class UserRoleAssignmentDto
{
    public Guid RoleId { get; set; }
    public string RoleCode { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
}
