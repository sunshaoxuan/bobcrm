using BobCrm.Api.Domain.Aggregates;
using BobCrm.Api.Domain.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

/// <summary>
/// 实体定义聚合服务
/// 负责聚合的保存、加载、验证等操作
/// </summary>
public class EntityDefinitionAggregateService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<EntityDefinitionAggregateService> _logger;

    public EntityDefinitionAggregateService(
        AppDbContext dbContext,
        ILogger<EntityDefinitionAggregateService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 加载聚合（包含主实体和所有子实体）
    /// </summary>
    public async Task<EntityDefinitionAggregate?> LoadAggregateAsync(
        Guid entityDefinitionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading aggregate for entity definition {EntityDefinitionId}", entityDefinitionId);

        // 加载主实体
        var root = await _dbContext.EntityDefinitions
            .Include(e => e.Fields.Where(f => f.SubEntityDefinitionId == null)) // 只加载主实体字段
            .Include(e => e.Interfaces)
            .FirstOrDefaultAsync(e => e.Id == entityDefinitionId, cancellationToken);

        if (root == null)
        {
            _logger.LogWarning("Entity definition {EntityDefinitionId} not found", entityDefinitionId);
            return null;
        }

        // 加载子实体
        var subEntities = await _dbContext.SubEntityDefinitions
            .Include(s => s.Fields)
            .Where(s => s.EntityDefinitionId == entityDefinitionId)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Loaded aggregate with {SubEntityCount} sub-entities", subEntities.Count);

        return new EntityDefinitionAggregate(root, subEntities);
    }

    /// <summary>
    /// 保存聚合（事务）
    /// </summary>
    public async Task<EntityDefinitionAggregate> SaveAggregateAsync(
        EntityDefinitionAggregate aggregate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saving aggregate for entity {EntityName}", aggregate.Root.EntityName);

        // 1. 验证聚合
        var validationResult = aggregate.Validate();
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Aggregate validation failed with {ErrorCount} errors", validationResult.Errors.Count);
            throw new ValidationException(validationResult.Errors);
        }

        // 2. 保存到数据库（事务）
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await SaveAggregateToDbAsync(aggregate, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Aggregate saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save aggregate, rolling back transaction");
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return aggregate;
    }

    /// <summary>
    /// 验证聚合（不保存）
    /// </summary>
    public Domain.Aggregates.ValidationResult ValidateAggregate(EntityDefinitionAggregate aggregate)
    {
        return aggregate.Validate();
    }

    /// <summary>
    /// 删除子实体
    /// </summary>
    public async Task DeleteSubEntityAsync(
        Guid subEntityId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting sub-entity {SubEntityId}", subEntityId);

        var subEntity = await _dbContext.SubEntityDefinitions
            .FirstOrDefaultAsync(s => s.Id == subEntityId, cancellationToken);

        if (subEntity != null)
        {
            _dbContext.SubEntityDefinitions.Remove(subEntity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Sub-entity {SubEntityId} deleted", subEntityId);
        }
    }

    private async Task SaveAggregateToDbAsync(
        EntityDefinitionAggregate aggregate,
        CancellationToken cancellationToken)
    {
        // 保存主实体
        var rootExists = await _dbContext.EntityDefinitions
            .AnyAsync(e => e.Id == aggregate.Root.Id, cancellationToken);

        if (rootExists)
        {
            _dbContext.EntityDefinitions.Update(aggregate.Root);
            _logger.LogDebug("Updating existing entity definition {EntityId}", aggregate.Root.Id);
        }
        else
        {
            _dbContext.EntityDefinitions.Add(aggregate.Root);
            _logger.LogDebug("Adding new entity definition {EntityId}", aggregate.Root.Id);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // 获取现有子实体IDs
        var existingSubEntityIds = await _dbContext.SubEntityDefinitions
            .Where(s => s.EntityDefinitionId == aggregate.Root.Id)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var newSubEntityIds = aggregate.SubEntities.Select(s => s.Id).ToList();

        // 删除已移除的子实体
        var subEntitiesToDelete = existingSubEntityIds.Except(newSubEntityIds).ToList();
        if (subEntitiesToDelete.Any())
        {
            _logger.LogDebug("Deleting {Count} removed sub-entities", subEntitiesToDelete.Count);
            await _dbContext.SubEntityDefinitions
                .Where(s => subEntitiesToDelete.Contains(s.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }

        // 保存或更新子实体
        foreach (var subEntity in aggregate.SubEntities)
        {
            var subEntityExists = existingSubEntityIds.Contains(subEntity.Id);

            if (subEntityExists)
            {
                _dbContext.SubEntityDefinitions.Update(subEntity);
                _logger.LogDebug("Updating sub-entity {Code}", subEntity.Code);
            }
            else
            {
                _dbContext.SubEntityDefinitions.Add(subEntity);
                _logger.LogDebug("Adding new sub-entity {Code}", subEntity.Code);
            }

            // 更新子实体的UpdatedAt
            subEntity.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Saved {Count} sub-entities", aggregate.SubEntities.Count);
    }
}
