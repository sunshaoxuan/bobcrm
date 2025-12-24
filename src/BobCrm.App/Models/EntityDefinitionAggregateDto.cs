namespace BobCrm.App.Models;

/// <summary>
/// 实体定义聚合DTO
/// </summary>
public class EntityDefinitionAggregateDto
{
    /// <summary>
    /// 主实体
    /// </summary>
    public EntityDefinitionDto Master { get; set; } = null!;

    /// <summary>
    /// 子实体列表
    /// </summary>
    public List<SubEntityDto> SubEntities { get; set; } = new();
}
