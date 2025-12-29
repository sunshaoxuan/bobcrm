using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Base.Models.Metadata;
using BobCrm.Api.Contracts.Requests.Entity;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using BobCrm.Api.Services.EntityLocking;
using BobCrm.Api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace BobCrm.Api.Tests.Services;

public class EntityLockServiceTests : IDisposable
{
    private readonly BobCrm.Api.Infrastructure.AppDbContext _db;
    private readonly Mock<ILogger<EntityLockService>> _loggerMock;
    private readonly EntityLockService _service;

    public EntityLockServiceTests()
    {
        _db = TestHelpers.CreateInMemoryDbContext(nameof(EntityLockServiceTests));
        _loggerMock = new Mock<ILogger<EntityLockService>>();
        _service = new EntityLockService(_db, _loggerMock.Object);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    [Fact]
    public async Task LockEntityAsync_ShouldLockEntity()
    {
        var entity = new EntityDefinition { EntityName = "TestEntity", IsLocked = false };
        _db.EntityDefinitions.Add(entity);
        await _db.SaveChangesAsync();

        var result = await _service.LockEntityAsync(entity.Id, "Testing lock");

        Assert.True(result);
        var updated = await _db.EntityDefinitions.FindAsync(entity.Id);
        Assert.True(updated!.IsLocked);
    }

    [Fact]
    public async Task LockEntityAsync_WhenAlreadyLocked_ShouldReturnTrue()
    {
        var entity = new EntityDefinition { EntityName = "TestEntity", IsLocked = true };
        _db.EntityDefinitions.Add(entity);
        await _db.SaveChangesAsync();

        var result = await _service.LockEntityAsync(entity.Id, "Already locked");

        Assert.True(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("is already locked")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task LockEntityAsync_WhenEntityNotFound_ShouldReturnFalse()
    {
        var result = await _service.LockEntityAsync(Guid.NewGuid(), "Not found");
        Assert.False(result);
    }

    [Fact]
    public async Task UnlockEntityAsync_ShouldUnlockEntity()
    {
        var entity = new EntityDefinition { EntityName = "LockedEntity", IsLocked = true };
        _db.EntityDefinitions.Add(entity);
        await _db.SaveChangesAsync();

        var result = await _service.UnlockEntityAsync(entity.Id, "Testing unlock");

        Assert.True(result);
        var updated = await _db.EntityDefinitions.FindAsync(entity.Id);
        Assert.False(updated!.IsLocked);
    }

    [Fact]
    public async Task UnlockEntityAsync_WhenNotLocked_ShouldReturnTrue()
    {
        var entity = new EntityDefinition { EntityName = "UnlockedEntity", IsLocked = false };
        _db.EntityDefinitions.Add(entity);
        await _db.SaveChangesAsync();

        var result = await _service.UnlockEntityAsync(entity.Id, "Already unlocked");
        Assert.True(result);
    }

    [Fact]
    public async Task LockEntityHierarchyAsync_ShouldLockAllChildren()
    {
        var root = new EntityDefinition { EntityName = "Root", IsLocked = false };
        var child = new EntityDefinition { EntityName = "Child", ParentEntityId = root.Id, IsLocked = false };
        var grandchild = new EntityDefinition { EntityName = "Grandchild", ParentEntityId = child.Id, IsLocked = false };
        _db.EntityDefinitions.AddRange(root, child, grandchild);
        await _db.SaveChangesAsync();

        // Fix parent relationships in memory if needed by EF, but ID matching should work
        child.ParentEntityId = root.Id;
        grandchild.ParentEntityId = child.Id;
        await _db.SaveChangesAsync();

        var count = await _service.LockEntityHierarchyAsync(root.Id, "Hierarchy lock");

        Assert.Equal(3, count);
        Assert.True((await _db.EntityDefinitions.FindAsync(root.Id))!.IsLocked);
        Assert.True((await _db.EntityDefinitions.FindAsync(child.Id))!.IsLocked);
        Assert.True((await _db.EntityDefinitions.FindAsync(grandchild.Id))!.IsLocked);
    }

    [Fact]
    public async Task CanModifyPropertyAsync_WhenLocked_ShouldRestrictCoreProperties()
    {
        var entity = new EntityDefinition { EntityName = "Locked", IsLocked = true };
        _db.EntityDefinitions.Add(entity);
        await _db.SaveChangesAsync();

        var (allowed1, reason1) = await _service.CanModifyPropertyAsync(entity.Id, nameof(EntityDefinition.EntityName));
        Assert.False(allowed1);
        Assert.Contains("不允许修改", reason1);

        var (allowed2, _) = await _service.CanModifyPropertyAsync(entity.Id, "Description");
        Assert.True(allowed2);
    }

    [Fact]
    public async Task GetLockInfoAsync_ShouldReturnReasons()
    {
        var entity = new EntityDefinition 
        { 
            EntityName = "Complex", 
            IsLocked = true, 
            Status = EntityStatus.Published,
            FullTypeName = "BobCrm.Entities.Complex"
        };
        _db.EntityDefinitions.Add(entity);
        
        // Add dependency
        _db.FormTemplates.Add(new FormTemplate { EntityType = "BobCrm.Entities.Complex", Name = "Tpl" });
        await _db.SaveChangesAsync();

        var info = await _service.GetLockInfoAsync(entity.Id);

        Assert.True(info.IsLocked);
        Assert.Contains(info.Reasons, r => r.Contains("表单模板引用"));
        // status check might fail if EF not tracking correctly, but let's see
    }

    [Fact]
    public async Task ValidateModificationAsync_ShouldDetectViolations()
    {
        var entity = new EntityDefinition { EntityName = "Original", IsLocked = true };
        _db.EntityDefinitions.Add(entity);
        await _db.SaveChangesAsync();

        var request = new EntityDefinitionUpdateRequest { EntityName = "Renamed" };
        var result = await _service.ValidateModificationAsync(entity.Id, request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("不允许修改实体名称"));
    }
}
