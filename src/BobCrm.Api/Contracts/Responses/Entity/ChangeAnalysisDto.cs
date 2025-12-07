namespace BobCrm.Api.Contracts.Responses.Entity;

/// <summary>
/// 变更分析 DTO
/// </summary>
public class ChangeAnalysisDto
{
    public int NewFieldsCount { get; set; }
    public int LengthIncreasesCount { get; set; }
    public bool HasDestructiveChanges { get; set; }
}
