using System.Text.Json;
using BobCrm.Api.Base.Aggregates;
using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Services;

/// <summary>
/// 聚合元数据发布服务
/// 将聚合信息发布为JSON元数据
/// </summary>
public class AggregateMetadataPublisher : IAggregateMetadataPublisher
{
    private readonly ILogger<AggregateMetadataPublisher> _logger;

    public AggregateMetadataPublisher(ILogger<AggregateMetadataPublisher> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 发布聚合的元数据
    /// </summary>
    public async Task PublishAsync(
        EntityDefinitionAggregate aggregate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publishing metadata for aggregate {EntityName}", aggregate.Root.EntityName);

        var metadataJson = GenerateMetadataJson(aggregate);

        _logger.LogDebug("Generated metadata JSON:\n{Json}", metadataJson);

        // TODO: 将元数据保存到配置中心或数据库
        // 可以存储在LocalizationResource表或专门的MetadataStore表中
        // 示例: await SaveToMetadataStoreAsync(aggregate.Root.Id, metadataJson, cancellationToken);

        await Task.CompletedTask;
    }

    /// <summary>
    /// 生成聚合元数据JSON
    /// </summary>
    public string GenerateMetadataJson(EntityDefinitionAggregate aggregate)
    {
        var metadata = new
        {
            entityDefinitionId = aggregate.Root.Id,
            @namespace = aggregate.Root.Namespace,
            entityName = aggregate.Root.EntityName,
            displayName = aggregate.Root.DisplayName,
            description = aggregate.Root.Description,
            structureType = "MasterDetail",
            publishedAt = DateTime.UtcNow,
            master = new
            {
                tableName = aggregate.Root.DefaultTableName,
                fields = aggregate.Root.Fields
                    .Where(f => f.SubEntityDefinitionId == null)
                    .OrderBy(f => f.SortOrder)
                    .Select(f => new
                    {
                        propertyName = f.PropertyName,
                        displayName = f.DisplayName,
                        dataType = f.DataType,
                        length = f.Length,
                        isRequired = f.IsRequired,
                        defaultValue = f.DefaultValue,
                        sortOrder = f.SortOrder
                    })
            },
            subEntities = aggregate.SubEntities
                .OrderBy(s => s.SortOrder)
                .Select(s => new
                {
                    code = s.Code,
                    displayName = s.DisplayName,
                    description = s.Description,
                    sortOrder = s.SortOrder,
                    foreignKeyField = s.ForeignKeyField ?? $"{aggregate.Root.EntityName}Id",
                    collectionPropertyName = s.CollectionPropertyName ?? s.Code,
                    tableName = $"{aggregate.Root.DefaultTableName}_{s.Code}",
                    fields = s.Fields
                        .OrderBy(f => f.SortOrder)
                        .Select(f => new
                        {
                            propertyName = f.PropertyName,
                            displayName = f.DisplayName,
                            dataType = f.DataType,
                            length = f.Length,
                            precision = f.Precision,
                            scale = f.Scale,
                            isRequired = f.IsRequired,
                            defaultValue = f.DefaultValue,
                            sortOrder = f.SortOrder
                        })
                })
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(metadata, options);
    }
}
