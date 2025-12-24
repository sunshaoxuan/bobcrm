namespace BobCrm.Api.Contracts.Responses.EntityAggregate;

/// <summary>
/// 聚合子实体信息。
/// </summary>
public class EntityAggregateSubEntityDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Dictionary<string, string?>? DisplayName { get; set; }
    public Dictionary<string, string?>? Description { get; set; }
    public int SortOrder { get; set; }
    public string? DefaultSortField { get; set; }
    public bool IsDescending { get; set; }
    public string? ForeignKeyField { get; set; }
    public string? CollectionPropertyName { get; set; }
    public string CascadeDeleteBehavior { get; set; } = string.Empty;
    public List<EntityAggregateFieldDto> Fields { get; set; } = new();
}

