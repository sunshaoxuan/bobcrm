namespace BobCrm.Api.Contracts.Responses.EntityAggregate;

/// <summary>
/// 子实体字段信息。
/// </summary>
public class EntityAggregateFieldDto
{
    public Guid Id { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public Dictionary<string, string?>? DisplayName { get; set; }
    public string DataType { get; set; } = string.Empty;
    public int? Length { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }
    public int SortOrder { get; set; }
}

