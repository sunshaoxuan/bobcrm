namespace BobCrm.App.Models;

public class UpdateUserRolesRequestDto
{
    public List<UserRoleAssignmentRequestDto> Roles { get; set; } = new();
}
