namespace BobCrm.App.Models;

public class UserDetailDto : UserSummaryDto
{
    public string? PhoneNumber { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
}
