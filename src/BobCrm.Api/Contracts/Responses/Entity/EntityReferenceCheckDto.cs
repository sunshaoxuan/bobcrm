namespace BobCrm.Api.Contracts.Responses.Entity;

/// <summary>
/// 实体引用检查结果 DTO
/// </summary>
public class EntityReferenceCheckDto
{
    public bool IsReferenced { get; set; }
    public int ReferenceCount { get; set; }
    public ReferenceDetailsDto ReferencedBy { get; set; } = new();
}
