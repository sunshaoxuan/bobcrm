namespace BobCrm.Api.Contracts.Responses.Auth;

public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public string Role { get; set; } = "User";
}
