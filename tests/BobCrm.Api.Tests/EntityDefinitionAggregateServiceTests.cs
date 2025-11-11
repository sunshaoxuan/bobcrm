using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using BobCrm.Api.Services;
using BobCrm.Api.Domain.Aggregates;
using BobCrm.Api.Domain.Models;
using BobCrm.Api.Infrastructure;

namespace BobCrm.Api.Tests;

/// <summary>
/// 实体定义聚合服务单元测试
/// 测试服务层的聚合管理逻辑
/// </summary>
public class EntityDefinitionAggregateServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly EntityDefinitionAggregateService _service;

    public EntityDefinitionAggregateServiceTests()
    {
        // 使用内存数据库
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);

        var mockLogger = new Mock<ILogger<EntityDefinitionAggregateService>>();
        _service = new EntityDefinitionAggregateService(_dbContext, mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    private EntityDefinition CreateTestEntity(string name = "TestEntity")
    {
        return new EntityDefinition
        {
            Id = Guid.NewGuid(),
            Namespace = "BobCrm.Domain.Test",
            EntityName = name,
            DisplayName = new Dictionary<string, string?> { ["zh"] = "测试实体" },
            Status = EntityStatus.Draft,
            StructureType = EntityStructureType.MasterDetail,
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task LoadAggregateAsync_ExistingEntity_ReturnsAggregate()
    {
        // Arrange
        var entity = CreateTestEntity();
        _dbContext.EntityDefinitions.Add(entity);

        var subEntity = new SubEntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = entity.Id,
            Code = "Lines",
            DisplayName = new Dictionary<string, string?> { ["zh"] = "明细" },
            SortOrder = 1
        };
        _dbContext.SubEntityDefinitions.Add(subEntity);

        await _dbContext.SaveChangesAsync();

        // Act
        var aggregate = await _service.LoadAggregateAsync(entity.Id);

        // Assert
        Assert.NotNull(aggregate);
        Assert.Equal(entity.Id, aggregate.Root.Id);
        Assert.Single(aggregate.SubEntities);
        Assert.Equal("Lines", aggregate.SubEntities.First().Code);
    }

    [Fact]
    public async Task LoadAggregateAsync_NonExistentEntity_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var aggregate = await _service.LoadAggregateAsync(nonExistentId);

        // Assert
        Assert.Null(aggregate);
    }

    [Fact]
    public async Task LoadAggregateAsync_WithSubEntityFields_LoadsFieldsCorrectly()
    {
        // Arrange
        var entity = CreateTestEntity();
        _dbContext.EntityDefinitions.Add(entity);

        var subEntity = new SubEntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = entity.Id,
            Code = "Lines",
            DisplayName = new Dictionary<string, string?> { ["zh"] = "明细" }
        };
        _dbContext.SubEntityDefinitions.Add(subEntity);

        var field = new FieldMetadata
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = entity.Id,
            SubEntityDefinitionId = subEntity.Id,
            PropertyName = "ProductCode",
            DisplayName = new Dictionary<string, string?> { ["zh"] = "产品编码" },
            DataType = FieldDataType.String
        };
        _dbContext.FieldMetadatas.Add(field);

        await _dbContext.SaveChangesAsync();

        // Act
        var aggregate = await _service.LoadAggregateAsync(entity.Id);

        // Assert
        Assert.NotNull(aggregate);
        var loadedSubEntity = aggregate.SubEntities.First();
        Assert.Single(loadedSubEntity.Fields);
        Assert.Equal("ProductCode", loadedSubEntity.Fields.First().PropertyName);
    }

    [Fact]
    public async Task SaveAggregateAsync_NewAggregate_SavesSuccessfully()
    {
        // Arrange
        var root = CreateTestEntity("NewOrder");
        var aggregate = new EntityDefinitionAggregate(root);
        aggregate.AddSubEntity("Lines", new Dictionary<string, string?> { ["zh"] = "明细" });

        // Act
        var savedAggregate = await _service.SaveAggregateAsync(aggregate);

        // Assert
        Assert.NotNull(savedAggregate);

        // 验证数据库
        var entityInDb = await _dbContext.EntityDefinitions.FindAsync(root.Id);
        Assert.NotNull(entityInDb);

        var subEntitiesInDb = await _dbContext.SubEntityDefinitions
            .Where(s => s.EntityDefinitionId == root.Id)
            .ToListAsync();
        Assert.Single(subEntitiesInDb);
    }

    [Fact]
    public async Task SaveAggregateAsync_UpdateExistingAggregate_UpdatesSuccessfully()
    {
        // Arrange - 首次保存
        var root = CreateTestEntity("Order");
        _dbContext.EntityDefinitions.Add(root);
        await _dbContext.SaveChangesAsync();

        // 创建聚合并添加子实体
        var aggregate = new EntityDefinitionAggregate(root);
        var subEntity = aggregate.AddSubEntity("Lines", new Dictionary<string, string?> { ["zh"] = "明细" });

        // Act - 保存聚合
        await _service.SaveAggregateAsync(aggregate);

        // Assert - 验证保存
        var subEntitiesInDb = await _dbContext.SubEntityDefinitions
            .Where(s => s.EntityDefinitionId == root.Id)
            .ToListAsync();
        Assert.Single(subEntitiesInDb);
    }

    [Fact]
    public async Task SaveAggregateAsync_InvalidAggregate_ThrowsValidationException()
    {
        // Arrange - 创建无效聚合（缺少EntityName）
        var root = CreateTestEntity();
        root.EntityName = ""; // 无效
        var aggregate = new EntityDefinitionAggregate(root);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _service.SaveAggregateAsync(aggregate));
    }

    [Fact]
    public async Task SaveAggregateAsync_WithFields_SavesFieldsCorrectly()
    {
        // Arrange
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);
        var subEntity = aggregate.AddSubEntity("Lines", new Dictionary<string, string?> { ["zh"] = "明细" });
        aggregate.AddFieldToSubEntity(
            subEntity.Id,
            "ProductCode",
            new Dictionary<string, string?> { ["zh"] = "产品" },
            FieldDataType.String,
            isRequired: true);

        // Act
        await _service.SaveAggregateAsync(aggregate);

        // Assert
        var fieldsInDb = await _dbContext.FieldMetadatas
            .Where(f => f.SubEntityDefinitionId == subEntity.Id)
            .ToListAsync();
        Assert.Single(fieldsInDb);
        Assert.Equal("ProductCode", fieldsInDb.First().PropertyName);
    }

    [Fact]
    public async Task SaveAggregateAsync_RemovingSubEntity_DeletesFromDatabase()
    {
        // Arrange - 首次保存包含两个子实体的聚合
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);
        var subEntity1 = aggregate.AddSubEntity("Lines", new Dictionary<string, string?> { ["zh"] = "明细" });
        var subEntity2 = aggregate.AddSubEntity("Comments", new Dictionary<string, string?> { ["zh"] = "评论" });
        await _service.SaveAggregateAsync(aggregate);

        // 重新加载聚合
        var loadedAggregate = await _service.LoadAggregateAsync(root.Id);
        Assert.NotNull(loadedAggregate);
        Assert.Equal(2, loadedAggregate.SubEntities.Count);

        // Act - 移除一个子实体后保存
        loadedAggregate.RemoveSubEntity(subEntity2.Id);
        await _service.SaveAggregateAsync(loadedAggregate);

        // Assert
        var subEntitiesInDb = await _dbContext.SubEntityDefinitions
            .Where(s => s.EntityDefinitionId == root.Id)
            .ToListAsync();
        Assert.Single(subEntitiesInDb);
        Assert.Equal("Lines", subEntitiesInDb.First().Code);
    }

    [Fact]
    public async Task ValidateAggregate_ValidAggregate_ReturnsValid()
    {
        // Arrange
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);
        aggregate.AddSubEntity("Lines", new Dictionary<string, string?> { ["zh"] = "明细" });

        // Act
        var result = _service.ValidateAggregate(aggregate);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAggregate_InvalidAggregate_ReturnsErrors()
    {
        // Arrange
        var root = CreateTestEntity();
        root.EntityName = ""; // 无效
        var aggregate = new EntityDefinitionAggregate(root);

        // Act
        var result = _service.ValidateAggregate(aggregate);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task DeleteSubEntityAsync_ExistingSubEntity_DeletesSuccessfully()
    {
        // Arrange
        var entity = CreateTestEntity();
        _dbContext.EntityDefinitions.Add(entity);

        var subEntity = new SubEntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = entity.Id,
            Code = "Lines",
            DisplayName = new Dictionary<string, string?> { ["zh"] = "明细" }
        };
        _dbContext.SubEntityDefinitions.Add(subEntity);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.DeleteSubEntityAsync(subEntity.Id);

        // Assert
        var deletedSubEntity = await _dbContext.SubEntityDefinitions.FindAsync(subEntity.Id);
        Assert.Null(deletedSubEntity);
    }

    [Fact]
    public async Task SaveAggregateAsync_TransactionRollback_OnException()
    {
        // Arrange - 创建一个会导致保存失败的聚合
        var root = CreateTestEntity();
        var aggregate = new EntityDefinitionAggregate(root);

        // 添加子实体
        var subEntity = aggregate.AddSubEntity("Lines", new Dictionary<string, string?> { ["zh"] = "明细" });

        // 首次保存
        await _service.SaveAggregateAsync(aggregate);

        // 制造冲突：直接在数据库中添加一个重复的子实体
        var duplicateSubEntity = new SubEntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = root.Id,
            Code = "Lines", // 重复的Code
            DisplayName = new Dictionary<string, string?> { ["zh"] = "重复" }
        };
        _dbContext.SubEntityDefinitions.Add(duplicateSubEntity);
        await _dbContext.SaveChangesAsync();

        // 重新加载聚合
        var reloadedAggregate = await _service.LoadAggregateAsync(root.Id);

        // Act - 尝试添加另一个同名子实体（应该在验证时失败）
        try
        {
            reloadedAggregate!.AddSubEntity("Lines", new Dictionary<string, string?> { ["zh"] = "又一个明细" });
            await _service.SaveAggregateAsync(reloadedAggregate);
            Assert.True(false, "应该抛出异常");
        }
        catch (DomainException)
        {
            // 预期的异常
        }

        // Assert - 验证数据库中只有原始的两个子实体（一个来自首次保存，一个是手动添加的重复）
        var subEntitiesInDb = await _dbContext.SubEntityDefinitions
            .Where(s => s.EntityDefinitionId == root.Id)
            .ToListAsync();
        Assert.Equal(2, subEntitiesInDb.Count);
    }

    [Fact]
    public async Task SaveAggregateAsync_UpdatesTimestamps()
    {
        // Arrange
        var root = CreateTestEntity();
        var originalUpdateTime = root.UpdatedAt;
        var aggregate = new EntityDefinitionAggregate(root);
        var subEntity = aggregate.AddSubEntity("Lines", new Dictionary<string, string?> { ["zh"] = "明细" });

        // Act
        await Task.Delay(10); // 确保时间戳不同
        await _service.SaveAggregateAsync(aggregate);

        // Assert
        Assert.NotNull(subEntity.UpdatedAt);
        Assert.True(subEntity.UpdatedAt > originalUpdateTime);
    }

    [Fact]
    public async Task LoadAggregateAsync_OrdersSubEntitiesBySortOrder()
    {
        // Arrange
        var entity = CreateTestEntity();
        _dbContext.EntityDefinitions.Add(entity);

        var subEntity1 = new SubEntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = entity.Id,
            Code = "Lines",
            DisplayName = new Dictionary<string, string?> { ["zh"] = "明细" },
            SortOrder = 2
        };

        var subEntity2 = new SubEntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityDefinitionId = entity.Id,
            Code = "Comments",
            DisplayName = new Dictionary<string, string?> { ["zh"] = "评论" },
            SortOrder = 1
        };

        _dbContext.SubEntityDefinitions.AddRange(subEntity1, subEntity2);
        await _dbContext.SaveChangesAsync();

        // Act
        var aggregate = await _service.LoadAggregateAsync(entity.Id);

        // Assert
        Assert.NotNull(aggregate);
        Assert.Equal(2, aggregate.SubEntities.Count);
        Assert.Equal("Comments", aggregate.SubEntities[0].Code); // SortOrder = 1
        Assert.Equal("Lines", aggregate.SubEntities[1].Code);    // SortOrder = 2
    }
}
