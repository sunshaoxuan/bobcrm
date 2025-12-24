namespace BobCrm.App.Models;

/// <summary>
/// 变更分析DTO
/// </summary>
public class ChangeAnalysisDto
{
    public List<FieldMetadataDto> NewFields { get; set; } = new();
    public Dictionary<string, int> LengthIncreases { get; set; } = new();
    public bool HasDestructiveChanges { get; set; }
}
