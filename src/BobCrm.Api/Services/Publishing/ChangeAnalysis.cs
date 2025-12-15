using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Services;

/// <summary>
/// 变更分析结果
/// </summary>
public class ChangeAnalysis
{
    public List<FieldMetadata> NewFields { get; set; } = new();
    public Dictionary<FieldMetadata, int> LengthIncreases { get; set; } = new();
    public Dictionary<FieldMetadata, int> LengthDecreases { get; set; } = new();
    public List<string> RemovedFields { get; set; } = new();
    public bool HasDestructiveChanges { get; set; }
}

