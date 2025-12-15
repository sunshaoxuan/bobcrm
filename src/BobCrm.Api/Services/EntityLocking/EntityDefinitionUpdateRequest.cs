namespace BobCrm.Api.Services.EntityLocking;

/// <summary>
/// 实体定义更新请求（用于验证）
/// </summary>
public class EntityDefinitionUpdateRequest
{
    public string? EntityName { get; set; }
    public string? Namespace { get; set; }
    public string? StructureType { get; set; }
    public string? DisplayNameKey { get; set; }
    public string? DescriptionKey { get; set; }
    public string? Icon { get; set; }
    public string? Category { get; set; }
    public int? Order { get; set; }
    public bool? IsEnabled { get; set; }
}
