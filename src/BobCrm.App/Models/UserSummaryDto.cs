namespace BobCrm.App.Models;

public class UserSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsLocked { get; set; }
    public List<UserRoleAssignmentDto> Roles { get; set; } = new();
}
