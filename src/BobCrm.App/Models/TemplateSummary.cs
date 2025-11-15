namespace BobCrm.App.Models;

public class TemplateSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public bool IsUserDefault { get; set; }
    public bool IsSystemDefault { get; set; }
}
