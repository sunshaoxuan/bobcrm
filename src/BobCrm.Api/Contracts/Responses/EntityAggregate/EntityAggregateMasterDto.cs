namespace BobCrm.Api.Contracts.Responses.EntityAggregate;

/// <summary>
/// 聚合主实体信息。
/// </summary>
public class EntityAggregateMasterDto
{
    public Guid Id { get; set; }
    public string Namespace { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public Dictionary<string, string?>? DisplayName { get; set; }
    public Dictionary<string, string?>? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

