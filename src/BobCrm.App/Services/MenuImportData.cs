namespace BobCrm.App.Services;

public record MenuImportData
{
    public string Version { get; init; } = "1.0";
    public DateTime ExportDate { get; init; }
    public List<MenuImportNode> Functions { get; init; } = new();
}
