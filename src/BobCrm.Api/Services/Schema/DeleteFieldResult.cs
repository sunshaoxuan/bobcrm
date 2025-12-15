namespace BobCrm.Api.Services;

/// <summary>
/// 字段删除结果
/// </summary>
public class DeleteFieldResult
{
    public Guid FieldId { get; set; }
    public bool Success { get; set; }
    public bool LogicalDeleteCompleted { get; set; }
    public bool PhysicalDeleteCompleted { get; set; }
    public string? ErrorMessage { get; set; }
}

