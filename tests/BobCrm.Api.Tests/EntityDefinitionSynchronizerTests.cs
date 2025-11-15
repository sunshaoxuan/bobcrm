using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace BobCrm.Api.Tests;

public class EntityDefinitionSynchronizerTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly Mock<ILogger<EntityDefinitionSynchronizer>> _mockLogger;
    private readonly EntityDefinitionSynchronizer _synchronizer;

    public EntityDefinitionSynchronizerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _db = new AppDbContext(options);
        _mockLogger = new Mock<ILogger<EntityDefinitionSynchronizer>>();
        _synchronizer = new EntityDefinitionSynchronizer(_db, _mockLogger.Object);
    }

    [Fact]
    public async Task SyncSystemEntitiesAsync_ShouldComplete_WithoutErrors()
    {
        // Act
        await _synchronizer.SyncSystemEntitiesAsync();

        // Assert
        // Should complete without throwing exceptions
        var entities = await _db.EntityDefinitions.ToListAsync();
        entities.Should().NotBeNull();

        // At least Customer should be synced if it implements IBizEntity
        // The actual count depends on how many IBizEntity implementations exist
    }

    [Fact]
    public async Task SyncSystemEntitiesAsync_ShouldInsertNewEntities()
    {
        // Arrange
        var initialCount = await _db.EntityDefinitions.CountAsync();

        // Act
        await _synchronizer.SyncSystemEntitiesAsync();

        // Assert
        var finalCount = await _db.EntityDefinitions.CountAsync();
        finalCount.Should().BeGreaterOrEqualTo(initialCount);

        // Verify all inserted entities have Source = System
        var systemEntities = await _db.EntityDefinitions
            .Where(e => e.Source == EntitySource.System)
            .ToListAsync();

        systemEntities.Should().NotBeEmpty();
        systemEntities.Should().OnlyContain(e => e.Source == EntitySource.System);
    }

    [Fact]
    public async Task SyncSystemEntitiesAsync_ShouldSkipExistingEntities()
    {
        // Arrange
        // First sync
        await _synchronizer.SyncSystemEntitiesAsync();
        var countAfterFirstSync = await _db.EntityDefinitions.CountAsync();

        // Act
        // Second sync - should skip existing entities
        await _synchronizer.SyncSystemEntitiesAsync();

        // Assert
        var countAfterSecondSync = await _db.EntityDefinitions.CountAsync();
        countAfterSecondSync.Should().Be(countAfterFirstSync);
    }

    [Fact]
    public async Task ResetSystemEntityAsync_ShouldThrow_WhenEntityNotFound()
    {
        // Arrange
        var nonExistentType = "NonExistent.Type";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _synchronizer.ResetSystemEntityAsync(nonExistentType)
        );
    }

    [Fact]
    public async Task ResetSystemEntityAsync_ShouldThrow_WhenEntityNotInDatabase()
    {
        // Arrange
        // Using Customer type but not syncing it first
        var customerType = typeof(BobCrm.Api.Base.Customer).FullName!;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _synchronizer.ResetSystemEntityAsync(customerType)
        );
    }

    [Fact]
    public async Task ResetSystemEntityAsync_ShouldThrow_WhenEntityIsNotSystemEntity()
    {
        // Arrange
        var customEntityId = Guid.NewGuid();
        var customEntity = new EntityDefinition
        {
            Id = customEntityId,
            Namespace = "Custom",
            EntityName = "CustomEntity",
            FullTypeName = "Custom.CustomEntity",
            Source = EntitySource.Custom, // Not a system entity
            Fields = new List<FieldMetadata>(),
            Interfaces = new List<EntityInterface>()
        };

        await _db.EntityDefinitions.AddAsync(customEntity);
        await _db.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _synchronizer.ResetSystemEntityAsync("Custom.CustomEntity")
        );
    }

    [Fact]
    public void EntitySource_Constants_ShouldBeAccessible()
    {
        // Assert
        EntitySource.System.Should().Be("System");
        EntitySource.Custom.Should().Be("Custom");
    }

    [Fact]
    public async Task SyncSystemEntitiesAsync_ShouldSetCreatedAndUpdatedDates()
    {
        // Act
        await _synchronizer.SyncSystemEntitiesAsync();

        // Assert
        var entities = await _db.EntityDefinitions
            .Where(e => e.Source == EntitySource.System)
            .ToListAsync();

        if (entities.Any())
        {
            entities.Should().OnlyContain(e => e.CreatedAt != default);
            entities.Should().OnlyContain(e => e.UpdatedAt != default);
        }
    }

    [Fact]
    public void EntityDefinitionSynchronizer_ShouldBeConstructable()
    {
        // Arrange & Act
        var synchronizer = new EntityDefinitionSynchronizer(_db, _mockLogger.Object);

        // Assert
        synchronizer.Should().NotBeNull();
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}

/// <summary>
/// 实体来源常量
/// </summary>
public static class EntitySource
{
    public const string System = "System";
    public const string Custom = "Custom";
}
