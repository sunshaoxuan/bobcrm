namespace BobCrm.App.Services;

public class FieldDefinitionDto
{
    public string key { get; set; } = string.Empty;
    public string label { get; set; } = string.Empty;
    public string type { get; set; } = string.Empty;
    public bool required { get; set; }
    public string? validation { get; set; }
    public string? defaultValue { get; set; }
    public string[] tags { get; set; } = Array.Empty<string>();
    public ActionDef[] actions { get; set; } = Array.Empty<ActionDef>();
}
