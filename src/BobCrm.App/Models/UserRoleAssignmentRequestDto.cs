namespace BobCrm.App.Models;

public class UserRoleAssignmentRequestDto
{
    public Guid RoleId { get; set; }
    public Guid? OrganizationId { get; set; }
}
