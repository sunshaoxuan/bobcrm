using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using BobCrm.Application.Templates;
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

    [Fact]
    public async Task SyncSystemEntitiesAsync_WhenExistingEntityMissingBasics_ShouldFillAndFixFields()
    {
        var customerType = typeof(BobCrm.Api.Base.Customer).FullName!;
        var customerId = Guid.NewGuid();

        await _db.EntityDefinitions.AddAsync(new EntityDefinition
        {
            Id = customerId,
            FullTypeName = customerType,
            Namespace = "",
            EntityName = "",
            EntityRoute = "",
            ApiEndpoint = "",
            DisplayName = null,
            Description = null,
            Source = EntitySource.Custom,
            Fields =
            [
                new FieldMetadata
                {
                    EntityDefinitionId = customerId,
                    PropertyName = "Code",
                    Source = FieldSource.Custom,
                    DataType = "",
                    SortOrder = 0,
                    DisplayName = null,
                    Length = null
                }
            ],
            Interfaces = new List<EntityInterface>()
        });
        await _db.SaveChangesAsync();

        await _synchronizer.SyncSystemEntitiesAsync();

        var updated = await _db.EntityDefinitions.Include(e => e.Fields)
            .FirstAsync(e => e.FullTypeName == customerType);

        updated.Source.Should().Be(EntitySource.System);
        updated.Namespace.Should().NotBeNullOrWhiteSpace();
        updated.EntityName.Should().NotBeNullOrWhiteSpace();
        updated.EntityRoute.Should().Be("customer");
        updated.ApiEndpoint.Should().Be("/api/customers");
        updated.DisplayName.Should().NotBeNull();

        updated.Fields.Should().Contain(f => f.PropertyName == "Id");
        updated.Fields.Should().Contain(f => f.PropertyName == "Name");
        updated.Fields.Should().Contain(f => f.PropertyName == "Version");
        updated.Fields.Should().Contain(f => f.PropertyName == "ExtData");

        var codeField = updated.Fields.First(f => f.PropertyName == "Code");
        codeField.Source.Should().Be(FieldSource.System);
        codeField.SortOrder.Should().BeGreaterThan(0);
        codeField.DataType.Should().NotBeNullOrWhiteSpace();
        codeField.DisplayName.Should().NotBeNull();
        codeField.Length.Should().NotBeNull();
    }

    [Fact]
    public async Task ResetSystemEntityAsync_ShouldRestoreDefaultDefinition()
    {
        await _synchronizer.SyncSystemEntitiesAsync();

        var customerType = typeof(BobCrm.Api.Base.Customer).FullName!;
        var customer = await _db.EntityDefinitions
            .Include(e => e.Fields)
            .Include(e => e.Interfaces)
            .FirstAsync(e => e.FullTypeName == customerType);

        customer.Namespace = "Broken.Namespace";
        customer.EntityRoute = "broken_route";
        customer.Fields.Add(new FieldMetadata
        {
            EntityDefinitionId = customer.Id,
            PropertyName = "CustomField",
            DataType = "String",
            Source = FieldSource.Custom,
            SortOrder = 999
        });
        await _db.SaveChangesAsync();

        await _synchronizer.ResetSystemEntityAsync(customerType);

        var reset = await _db.EntityDefinitions
            .Include(e => e.Fields)
            .Include(e => e.Interfaces)
            .FirstAsync(e => e.FullTypeName == customerType);

        reset.EntityRoute.Should().Be("customer");
        reset.Namespace.Should().Be("BobCrm.Api.Base");
        reset.Source.Should().Be(EntitySource.System);

        reset.Fields.Should().NotContain(f => f.PropertyName == "CustomField");
        reset.Fields.Should().Contain(f => f.PropertyName == "Id");
        reset.Fields.Should().Contain(f => f.PropertyName == "Code");
        reset.Interfaces.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SyncSystemEntitiesAsync_WithTemplateService_ShouldUpsertTemplateStateBindings()
    {
        var templateService = new Mock<IDefaultTemplateService>();
        templateService
            .Setup(s => s.EnsureTemplatesAsync(It.IsAny<EntityDefinition>(), It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EntityDefinition entityDef, string? _, bool _, CancellationToken _) =>
            {
                var result = new DefaultTemplateGenerationResult();
                result.Templates["List"] = new FormTemplate { Id = 99, Name = "List", EntityType = entityDef.EntityRoute, UserId = "system", LayoutJson = "{}" };
                return result;
            });

        var bindingLogger = new Mock<ILogger<TemplateBindingService>>();
        var bindingService = new TemplateBindingService(_db, bindingLogger.Object);
        var synchronizer = new EntityDefinitionSynchronizer(_db, _mockLogger.Object, templateService.Object, bindingService);

        _db.TemplateStateBindings.Add(new TemplateStateBinding
        {
            EntityType = "customer",
            ViewState = "List",
            TemplateId = 1,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });
        await _db.SaveChangesAsync();

        await synchronizer.SyncSystemEntitiesAsync();

        var binding = await _db.TemplateStateBindings.FirstAsync(b => b.EntityType == "customer" && b.ViewState == "List" && b.IsDefault);
        binding.TemplateId.Should().Be(99);
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}
