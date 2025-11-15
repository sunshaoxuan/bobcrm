using BobCrm.Api.Base.Aggregates;
using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Services;

/// <summary>
/// 子实体代码生成服务接口
/// </summary>
public interface ISubEntityCodeGenerator
{
    /// <summary>
    /// 为聚合中的所有子实体生成C#类
    /// </summary>
    Task GenerateSubEntitiesAsync(
        EntityDefinitionAggregate aggregate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成单个子实体的C#类代码
    /// </summary>
    string GenerateSubEntityClass(
        EntityDefinition mainEntity,
        SubEntityDefinition subEntity);

    /// <summary>
    /// 生成AggVO类代码（主实体 + 子实体列表）
    /// </summary>
    string GenerateAggregateVoClass(
        EntityDefinition mainEntity,
        List<SubEntityDefinition> subEntities);
}
