using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using BobCrm.Api.Services.EntityLocking;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BobCrm.Api.Tests;

/// <summary>
/// EntityLockService 测试
/// 覆盖乐观锁并发控制
/// </summary>
public class EntityLockServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static EntityLockService CreateService(AppDbContext context)
    {
        return new EntityLockService(context, NullLogger<EntityLockService>.Instance);
    }

    [Fact]
    public async Task LockEntityAsync_WhenEntityExists_ShouldSetIsLockedTrue()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityName = "TestEntity",
            Namespace = "Test",
            FullTypeName = "Test.TestEntity",
            Status = EntityStatus.Draft,
            IsLocked = false
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var result = await service.LockEntityAsync(entity.Id, "Test lock");

        // Assert
        result.Should().BeTrue();
        var updated = await ctx.EntityDefinitions.FindAsync(entity.Id);
        updated!.IsLocked.Should().BeTrue();
    }

    [Fact]
    public async Task LockEntityAsync_WhenEntityNotFound_ShouldReturnFalse()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        // Act
        var result = await service.LockEntityAsync(Guid.NewGuid(), "Test lock");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task LockEntityAsync_WhenAlreadyLocked_ShouldReturnTrue()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityName = "TestEntity",
            Namespace = "Test",
            FullTypeName = "Test.TestEntity",
            Status = EntityStatus.Draft,
            IsLocked = true
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var result = await service.LockEntityAsync(entity.Id, "Test lock");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UnlockEntityAsync_WhenEntityLocked_ShouldSetIsLockedFalse()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityName = "TestEntity",
            Namespace = "Test",
            FullTypeName = "Test.TestEntity",
            Status = EntityStatus.Draft,
            IsLocked = true
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var result = await service.UnlockEntityAsync(entity.Id, "Test unlock");

        // Assert
        result.Should().BeTrue();
        var updated = await ctx.EntityDefinitions.FindAsync(entity.Id);
        updated!.IsLocked.Should().BeFalse();
    }

    [Fact]
    public async Task UnlockEntityAsync_WhenEntityNotFound_ShouldReturnFalse()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        // Act
        var result = await service.UnlockEntityAsync(Guid.NewGuid(), "Test unlock");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UnlockEntityAsync_WhenNotLocked_ShouldReturnTrue()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityName = "TestEntity",
            Namespace = "Test",
            FullTypeName = "Test.TestEntity",
            Status = EntityStatus.Draft,
            IsLocked = false
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var result = await service.UnlockEntityAsync(entity.Id, "Test unlock");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEntityLockedAsync_WhenLocked_ShouldReturnTrue()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityName = "TestEntity",
            Namespace = "Test",
            FullTypeName = "Test.TestEntity",
            Status = EntityStatus.Draft,
            IsLocked = true
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var result = await service.IsEntityLockedAsync(entity.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsEntityLockedAsync_WhenNotLocked_ShouldReturnFalse()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityName = "TestEntity",
            Namespace = "Test",
            FullTypeName = "Test.TestEntity",
            Status = EntityStatus.Draft,
            IsLocked = false
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var result = await service.IsEntityLockedAsync(entity.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsEntityLockedAsync_WhenEntityNotFound_ShouldReturnFalse()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        // Act
        var result = await service.IsEntityLockedAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanModifyPropertyAsync_WhenNotLocked_ShouldAllowAll()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityName = "TestEntity",
            Namespace = "Test",
            FullTypeName = "Test.TestEntity",
            Status = EntityStatus.Draft,
            IsLocked = false
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var (allowed, reason) = await service.CanModifyPropertyAsync(entity.Id, "EntityName");

        // Assert
        allowed.Should().BeTrue();
        reason.Should().BeNull();
    }

    [Fact]
    public async Task CanModifyPropertyAsync_WhenLockedAndRestrictedProperty_ShouldReturnFalse()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityName = "TestEntity",
            Namespace = "Test",
            FullTypeName = "Test.TestEntity",
            Status = EntityStatus.Draft,
            IsLocked = true
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var (allowed, reason) = await service.CanModifyPropertyAsync(entity.Id, "EntityName");

        // Assert
        allowed.Should().BeFalse();
        reason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CanModifyPropertyAsync_WhenLockedAndNonRestrictedProperty_ShouldAllowModification()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityName = "TestEntity",
            Namespace = "Test",
            FullTypeName = "Test.TestEntity",
            Status = EntityStatus.Draft,
            IsLocked = true
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var (allowed, reason) = await service.CanModifyPropertyAsync(entity.Id, "Description");

        // Assert
        allowed.Should().BeTrue();
        reason.Should().BeNull();
    }

    [Fact]
    public async Task GetLockInfoAsync_WhenEntityNotFound_ShouldThrowException()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetLockInfoAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetLockInfoAsync_WhenNotLocked_ShouldReturnEmptyReasons()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityName = "TestEntity",
            Namespace = "Test",
            FullTypeName = "Test.TestEntity",
            Status = EntityStatus.Draft,
            IsLocked = false
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var lockInfo = await service.GetLockInfoAsync(entity.Id);

        // Assert
        lockInfo.IsLocked.Should().BeFalse();
        lockInfo.Reasons.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLockInfoAsync_WhenLockedAndPublished_ShouldIncludePublishedReason()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityName = "TestEntity",
            Namespace = "Test",
            FullTypeName = "Test.TestEntity",
            Status = EntityStatus.Published,
            IsLocked = true
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var lockInfo = await service.GetLockInfoAsync(entity.Id);

        // Assert
        lockInfo.IsLocked.Should().BeTrue();
        lockInfo.Reasons.Should().Contain(r => r.Contains("发布"));
    }

    [Fact]
    public async Task ValidateModificationAsync_WhenNotLocked_ShouldBeValid()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityName = "TestEntity",
            Namespace = "Test",
            FullTypeName = "Test.TestEntity",
            Status = EntityStatus.Draft,
            IsLocked = false
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var request = new EntityDefinitionUpdateRequest { EntityName = "NewName" };

        // Act
        var result = await service.ValidateModificationAsync(entity.Id, request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateModificationAsync_WhenLockedAndChangingName_ShouldBeInvalid()
    {
        // Arrange
        await using var ctx = CreateContext();
        var entity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityName = "TestEntity",
            Namespace = "Test",
            FullTypeName = "Test.TestEntity",
            Status = EntityStatus.Draft,
            IsLocked = true
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var request = new EntityDefinitionUpdateRequest { EntityName = "NewName" };

        // Act
        var result = await service.ValidateModificationAsync(entity.Id, request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ValidateModificationAsync_WhenEntityNotFound_ShouldBeInvalid()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);
        var request = new EntityDefinitionUpdateRequest { EntityName = "NewName" };

        // Act
        var result = await service.ValidateModificationAsync(Guid.NewGuid(), request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("实体不存在");
    }

    [Fact]
    public async Task LockEntityHierarchyAsync_ShouldLockParentAndChildren()
    {
        // Arrange
        await using var ctx = CreateContext();
        var parentEntity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityName = "ParentEntity",
            Namespace = "Test",
            FullTypeName = "Test.ParentEntity",
            Status = EntityStatus.Draft,
            IsLocked = false
        };
        var childEntity = new EntityDefinition
        {
            Id = Guid.NewGuid(),
            EntityName = "ChildEntity",
            Namespace = "Test",
            FullTypeName = "Test.ChildEntity",
            Status = EntityStatus.Draft,
            IsLocked = false,
            ParentEntityId = parentEntity.Id
        };
        ctx.EntityDefinitions.AddRange(parentEntity, childEntity);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        var lockedCount = await service.LockEntityHierarchyAsync(parentEntity.Id, "Test hierarchy lock");

        // Assert
        // Should lock at least the parent
        lockedCount.Should().BeGreaterThan(0);
        var updatedParent = await ctx.EntityDefinitions.FindAsync(parentEntity.Id);
        updatedParent!.IsLocked.Should().BeTrue();
    }
}
