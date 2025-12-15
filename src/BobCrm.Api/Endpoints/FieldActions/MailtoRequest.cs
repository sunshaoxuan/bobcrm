namespace BobCrm.Api.Endpoints;

/// <summary>
/// Mailto请求
/// </summary>
public record MailtoRequest
{
    public string Email { get; init; } = string.Empty;
    public string? Subject { get; init; }
    public string? Body { get; init; }
    public string? Cc { get; init; }
    public string? Bcc { get; init; }
}

