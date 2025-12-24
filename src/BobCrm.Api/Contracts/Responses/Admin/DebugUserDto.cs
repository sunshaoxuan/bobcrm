namespace BobCrm.Api.Contracts.Responses.Admin;

/// <summary>
/// 调试用用户信息（开发环境）。
/// </summary>
public class DebugUserDto
{
    public string Id { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool HasPassword { get; set; }
}

