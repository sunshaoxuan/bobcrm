namespace BobCrm.App.Services;

public record ImportErrorResponse
{
    public string Error { get; init; } = string.Empty;
    public List<string> Conflicts { get; init; } = new();
    public string Message { get; init; } = string.Empty;
}
