namespace BobCrm.Api.Contracts.Responses.EntityAggregate;

/// <summary>
/// 实体聚合响应（主实体 + 子实体）。
/// </summary>
public class EntityAggregateDto
{
    public EntityAggregateMasterDto Master { get; set; } = new();
    public List<EntityAggregateSubEntityDto> SubEntities { get; set; } = new();
}

