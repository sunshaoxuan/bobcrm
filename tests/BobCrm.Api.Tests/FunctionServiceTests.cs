using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.Requests.Access;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BobCrm.Api.Tests;

/// <summary>
/// FunctionService 测试
/// 覆盖功能节点管理
/// </summary>
public class FunctionServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static FunctionService CreateService(AppDbContext context)
    {
        var multilingual = new MultilingualFieldService(context, NullLogger<MultilingualFieldService>.Instance);
        return new FunctionService(context, multilingual);
    }

    #region CreateFunctionAsync Tests

    [Fact]
    public async Task CreateFunctionAsync_WithValidRequest_ShouldCreateFunction()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);
        var request = new CreateFunctionRequest
        {
            Code = "TEST.FUNCTION",
            Name = "Test Function",
            IsMenu = true,
            SortOrder = 1
        };

        // Act
        var result = await service.CreateFunctionAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Code.Should().Be("TEST.FUNCTION");
        result.Name.Should().Be("Test Function");
        result.IsMenu.Should().BeTrue();
    }

    [Fact]
    public async Task CreateFunctionAsync_WithEmptyCode_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);
        var request = new CreateFunctionRequest
        {
            Code = "",
            Name = "Test Function"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateFunctionAsync(request));
    }

    [Fact]
    public async Task CreateFunctionAsync_WithDuplicateCode_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        ctx.FunctionNodes.Add(new FunctionNode { Code = "TEST.FUNCTION", Name = "Existing" });
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var request = new CreateFunctionRequest
        {
            Code = "TEST.FUNCTION",
            Name = "Test Function"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateFunctionAsync(request));
        ex.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateFunctionAsync_WithParentId_ShouldSetParent()
    {
        // Arrange
        await using var ctx = CreateContext();
        var parent = new FunctionNode { Code = "PARENT", Name = "Parent" };
        ctx.FunctionNodes.Add(parent);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var request = new CreateFunctionRequest
        {
            Code = "CHILD",
            Name = "Child Function",
            ParentId = parent.Id
        };

        // Act
        var result = await service.CreateFunctionAsync(request);

        // Assert
        result.ParentId.Should().Be(parent.Id);
    }

    [Fact]
    public async Task CreateFunctionAsync_WithRoute_ShouldSetRoute()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);
        var request = new CreateFunctionRequest
        {
            Code = "TEST.FUNCTION",
            Name = "Test Function",
            Route = "/test/route"
        };

        // Act
        var result = await service.CreateFunctionAsync(request);

        // Assert
        result.Route.Should().Be("/test/route");
    }

    [Fact]
    public async Task CreateFunctionAsync_WithIcon_ShouldSetIcon()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);
        var request = new CreateFunctionRequest
        {
            Code = "TEST.FUNCTION",
            Name = "Test Function",
            Icon = "home"
        };

        // Act
        var result = await service.CreateFunctionAsync(request);

        // Assert
        result.Icon.Should().Be("home");
    }

    #endregion

    #region UpdateFunctionAsync Tests

    [Fact]
    public async Task UpdateFunctionAsync_WithValidRequest_ShouldUpdateFunction()
    {
        // Arrange
        await using var ctx = CreateContext();
        var function = new FunctionNode { Code = "TEST.FUNCTION", Name = "Original" };
        ctx.FunctionNodes.Add(function);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var request = new UpdateFunctionRequest
        {
            Name = "Updated Name"
        };

        // Act
        var result = await service.UpdateFunctionAsync(function.Id, request);

        // Assert
        result.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateFunctionAsync_WithNonExistentId_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);
        var request = new UpdateFunctionRequest { Name = "Updated" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateFunctionAsync(Guid.NewGuid(), request));
    }

    [Fact]
    public async Task UpdateFunctionAsync_SettingSelfAsParent_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var function = new FunctionNode { Code = "TEST.FUNCTION", Name = "Test" };
        ctx.FunctionNodes.Add(function);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var request = new UpdateFunctionRequest { ParentId = function.Id };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateFunctionAsync(function.Id, request));
        ex.Message.Should().Contain("own parent");
    }

    [Fact]
    public async Task UpdateFunctionAsync_WithNonExistentParent_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var function = new FunctionNode { Code = "TEST.FUNCTION", Name = "Test" };
        ctx.FunctionNodes.Add(function);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var request = new UpdateFunctionRequest { ParentId = Guid.NewGuid() };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateFunctionAsync(function.Id, request));
        ex.Message.Should().Contain("not exist");
    }

    [Fact]
    public async Task UpdateFunctionAsync_ClearParent_ShouldSetParentToNull()
    {
        // Arrange
        await using var ctx = CreateContext();
        var parent = new FunctionNode { Code = "PARENT", Name = "Parent" };
        var child = new FunctionNode { Code = "CHILD", Name = "Child", ParentId = parent.Id };
        ctx.FunctionNodes.AddRange(parent, child);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var request = new UpdateFunctionRequest { ClearParent = true };

        // Act
        var result = await service.UpdateFunctionAsync(child.Id, request);

        // Assert
        result.ParentId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateFunctionAsync_UpdateRoute_ShouldChangeRoute()
    {
        // Arrange
        await using var ctx = CreateContext();
        var function = new FunctionNode { Code = "TEST.FUNCTION", Name = "Test", Route = "/old/route" };
        ctx.FunctionNodes.Add(function);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var request = new UpdateFunctionRequest { Route = "/new/route" };

        // Act
        var result = await service.UpdateFunctionAsync(function.Id, request);

        // Assert
        result.Route.Should().Be("/new/route");
    }

    [Fact]
    public async Task UpdateFunctionAsync_ClearRoute_ShouldSetRouteToNull()
    {
        // Arrange
        await using var ctx = CreateContext();
        var function = new FunctionNode { Code = "TEST.FUNCTION", Name = "Test", Route = "/old/route" };
        ctx.FunctionNodes.Add(function);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var request = new UpdateFunctionRequest { ClearRoute = true };

        // Act
        var result = await service.UpdateFunctionAsync(function.Id, request);

        // Assert
        result.Route.Should().BeNull();
    }

    #endregion

    #region DeleteFunctionAsync Tests

    [Fact]
    public async Task DeleteFunctionAsync_WithValidId_ShouldDeleteFunction()
    {
        // Arrange
        await using var ctx = CreateContext();
        var function = new FunctionNode { Code = "TEST.FUNCTION", Name = "Test" };
        ctx.FunctionNodes.Add(function);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act
        await service.DeleteFunctionAsync(function.Id);

        // Assert
        var deleted = await ctx.FunctionNodes.FindAsync(function.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteFunctionAsync_WithNonExistentId_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        // Act & Assert - service throws when function not found
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteFunctionAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteFunctionAsync_WithChildren_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var parent = new FunctionNode { Code = "PARENT", Name = "Parent" };
        ctx.FunctionNodes.Add(parent);
        await ctx.SaveChangesAsync();

        var child = new FunctionNode { Code = "CHILD", Name = "Child", ParentId = parent.Id };
        ctx.FunctionNodes.Add(child);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteFunctionAsync(parent.Id));
        ex.Message.Should().Contain("children");
    }

    #endregion

    #region GetMyFunctionsAsync Tests

    [Fact]
    public async Task GetMyFunctionsAsync_WithEmptyUserId_ShouldThrow()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetMyFunctionsAsync(""));
    }

    [Fact]
    public async Task GetMyFunctionsAsync_WithNoAssignments_ShouldReturnEmpty()
    {
        // Arrange
        await using var ctx = CreateContext();
        var service = CreateService(ctx);

        // Act
        var result = await service.GetMyFunctionsAsync("user1");

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}
