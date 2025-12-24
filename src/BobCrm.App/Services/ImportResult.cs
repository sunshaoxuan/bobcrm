namespace BobCrm.App.Services;

public record ImportResult
{
    public string Message { get; init; } = string.Empty;
    public int Imported { get; init; }
    public int Skipped { get; init; }
}
