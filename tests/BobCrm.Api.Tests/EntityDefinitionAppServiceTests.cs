using BobCrm.Api.Abstractions;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.Requests.Entity;
using BobCrm.Api.Contracts.Responses.Entity;
using BobCrm.Api.Core.DomainCommon;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BobCrm.Api.Tests;

/// <summary>
/// EntityDefinitionAppService 测试
/// 覆盖实体定义应用层
/// </summary>
public class EntityDefinitionAppServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static EntityDefinitionAppService CreateService(AppDbContext context)
    {
        var mockLoc = new Mock<ILocalization>();
        mockLoc.Setup(l => l.T(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((key, lang) => key);

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(new DefaultHttpContext());

        return new EntityDefinitionAppService(
            context,
            mockLoc.Object,
            Mock.Of<IFieldMetadataCache>(),
            NullLogger<EntityDefinitionAppService>.Instance,
            mockHttpContextAccessor.Object);
    }

    private static MultilingualText CreateMultilingualText(string zhValue)
    {
        var text = new MultilingualText();
        text["zh"] = zhValue;
        return text;
    }

    #region CreateEntityDefinitionAsync Tests

    [Fact]
    public async Task CreateEntityDefinitionAsync_WithValidRequest_ShouldCreateEntity()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);
        var dto = new CreateEntityDefinitionDto
        {
            Namespace = "Test",
            EntityName = "Customer",
            DisplayName = CreateMultilingualText("客户")
        };

        // Act
        var result = await service.CreateEntityDefinitionAsync("user1", "zh", dto);

        // Assert
        result.Should().NotBeNull();
        result.EntityName.Should().Be("Customer");
        result.Namespace.Should().Be("Test");
    }

    [Fact]
    public async Task CreateEntityDefinitionAsync_WithEmptyNamespace_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);
        var dto = new CreateEntityDefinitionDto
        {
            Namespace = "",
            EntityName = "Customer",
            DisplayName = CreateMultilingualText("客户")
        };

        // Act & Assert
        await Assert.ThrowsAsync<ServiceException>(() =>
            service.CreateEntityDefinitionAsync("user1", "zh", dto));
    }

    [Fact]
    public async Task CreateEntityDefinitionAsync_WithEmptyEntityName_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);
        var dto = new CreateEntityDefinitionDto
        {
            Namespace = "Test",
            EntityName = "",
            DisplayName = CreateMultilingualText("客户")
        };

        // Act & Assert
        await Assert.ThrowsAsync<ServiceException>(() =>
            service.CreateEntityDefinitionAsync("user1", "zh", dto));
    }

    [Fact]
    public async Task CreateEntityDefinitionAsync_WithEmptyDisplayName_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);
        var dto = new CreateEntityDefinitionDto
        {
            Namespace = "Test",
            EntityName = "Customer",
            DisplayName = new MultilingualText()
        };

        // Act & Assert
        await Assert.ThrowsAsync<ServiceException>(() =>
            service.CreateEntityDefinitionAsync("user1", "zh", dto));
    }

    [Fact]
    public async Task CreateEntityDefinitionAsync_WithDuplicateEntity_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        ctx.EntityDefinitions.Add(new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "Customer",
            FullTypeName = "Test.Customer",
            Status = EntityStatus.Draft
        });
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var dto = new CreateEntityDefinitionDto
        {
            Namespace = "Test",
            EntityName = "Customer",
            DisplayName = CreateMultilingualText("客户")
        };

        // Act & Assert
        await Assert.ThrowsAsync<ServiceException>(() =>
            service.CreateEntityDefinitionAsync("user1", "zh", dto));
    }

    [Fact]
    public async Task CreateEntityDefinitionAsync_ShouldSetCreatedBy()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);
        var dto = new CreateEntityDefinitionDto
        {
            Namespace = "Test",
            EntityName = "Customer",
            DisplayName = CreateMultilingualText("客户")
        };

        // Act
        var result = await service.CreateEntityDefinitionAsync("user1", "zh", dto);

        // Assert
        var entity = await ctx.EntityDefinitions.FirstOrDefaultAsync(e => e.Id == result.Id);
        entity!.CreatedBy.Should().Be("user1");
    }

    #endregion

    #region UpdateEntityDefinitionAsync Tests

    [Fact]
    public async Task UpdateEntityDefinitionAsync_WithValidRequest_ShouldUpdateEntity()
    {
        // Arrange
        await using var ctx = CreateContext();
        var displayName = CreateMultilingualText("客户");
        var entity = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "Customer",
            FullTypeName = "Test.Customer",
            DisplayName = displayName,
            Status = EntityStatus.Draft
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var newDisplayName = CreateMultilingualText("更新后的客户");
        var dto = new UpdateEntityDefinitionDto
        {
            DisplayName = newDisplayName
        };

        // Act
        var result = await service.UpdateEntityDefinitionAsync(entity.Id, "user1", "zh", dto);

        // Assert - result is not null
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateEntityDefinitionAsync_WithNonExistentId_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);
        var dto = new UpdateEntityDefinitionDto
        {
            DisplayName = CreateMultilingualText("更新")
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.UpdateEntityDefinitionAsync(Guid.NewGuid(), "user1", "zh", dto));
    }

    [Fact]
    public async Task UpdateEntityDefinitionAsync_WhenLocked_ShouldPreventNamespaceChange()
    {
        // Arrange
        await using var ctx = CreateContext();
        var displayName = CreateMultilingualText("客户");
        var entity = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "Customer",
            FullTypeName = "Test.Customer",
            DisplayName = displayName,
            Status = EntityStatus.Draft,
            IsLocked = true
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var dto = new UpdateEntityDefinitionDto
        {
            Namespace = "NewNamespace"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ServiceException>(() =>
            service.UpdateEntityDefinitionAsync(entity.Id, "user1", "zh", dto));
    }

    #endregion

    #region Additional Update Tests

    [Fact]
    public async Task UpdateEntityDefinitionAsync_WhenLocked_ShouldPreventEntityNameChange()
    {
        // Arrange
        await using var ctx = CreateContext();
        var displayName = CreateMultilingualText("客户");
        var entity = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "Customer",
            FullTypeName = "Test.Customer",
            DisplayName = displayName,
            Status = EntityStatus.Draft,
            IsLocked = true
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var dto = new UpdateEntityDefinitionDto
        {
            EntityName = "NewCustomer"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ServiceException>(() =>
            service.UpdateEntityDefinitionAsync(entity.Id, "user1", "zh", dto));
    }

    [Fact]
    public async Task UpdateEntityDefinitionAsync_ShouldAllowDisplayNameChangeWhenLocked()
    {
        // Arrange
        await using var ctx = CreateContext();
        var displayName = CreateMultilingualText("客户");
        var entity = new EntityDefinition
        {
            Namespace = "Test",
            EntityName = "Customer",
            FullTypeName = "Test.Customer",
            DisplayName = displayName,
            Status = EntityStatus.Draft,
            IsLocked = true
        };
        ctx.EntityDefinitions.Add(entity);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var newDisplayName = CreateMultilingualText("更新后的客户");
        var dto = new UpdateEntityDefinitionDto
        {
            DisplayName = newDisplayName
        };

        // Act
        var result = await service.UpdateEntityDefinitionAsync(entity.Id, "user1", "zh", dto);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion
}
