namespace BobCrm.App.Models;

public class CreateUserRequestDto
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public bool EmailConfirmed { get; set; } = true;
    public List<UserRoleAssignmentRequestDto> Roles { get; set; } = new();
}
