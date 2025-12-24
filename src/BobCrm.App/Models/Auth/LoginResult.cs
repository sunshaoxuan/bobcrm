namespace BobCrm.App.Models.Auth;

public record LoginResult(string accessToken, string refreshToken, UserDto user);

