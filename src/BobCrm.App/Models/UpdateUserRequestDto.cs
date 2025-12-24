namespace BobCrm.App.Models;

public class UpdateUserRequestDto
{
    public string? Email { get; set; }
    public bool? EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public bool? IsLocked { get; set; }
    public string? Password { get; set; }
}
