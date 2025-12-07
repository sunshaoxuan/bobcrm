namespace BobCrm.Api.Contracts.Responses.Auth;

public class SessionResponse
{
    public bool Valid { get; set; }
    public UserInfo? User { get; set; }
}
