namespace BobCrm.App.Models;

/// <summary>
/// 子实体DTO
/// </summary>
public class SubEntityDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Dictionary<string, string?> DisplayName { get; set; } = new();
    public Dictionary<string, string?>? Description { get; set; }
    public int SortOrder { get; set; }
    public string? DefaultSortField { get; set; }
    public bool IsDescending { get; set; }
    public string? ForeignKeyField { get; set; }
    public string? CollectionPropertyName { get; set; }
    public string CascadeDeleteBehavior { get; set; } = "Cascade";
    public List<FieldMetadataDto> Fields { get; set; } = new();
}
