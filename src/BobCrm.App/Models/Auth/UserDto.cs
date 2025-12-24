namespace BobCrm.App.Models.Auth;

public record UserDto(string id, string userName, string email, bool emailConfirmed, string role);

