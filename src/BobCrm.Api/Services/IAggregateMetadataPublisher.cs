using BobCrm.Api.Base.Aggregates;

namespace BobCrm.Api.Services;

/// <summary>
/// 聚合元数据发布服务接口
/// </summary>
public interface IAggregateMetadataPublisher
{
    /// <summary>
    /// 发布聚合的元数据（包含主实体和子实体）
    /// </summary>
    Task PublishAsync(
        EntityDefinitionAggregate aggregate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 生成聚合元数据JSON
    /// </summary>
    string GenerateMetadataJson(EntityDefinitionAggregate aggregate);
}
