using System.Security.Claims;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Middleware;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BobCrm.Api.Tests;

/// <summary>
/// FunctionPermissionFilter 测试
/// 覆盖端点级权限过滤
/// </summary>
public class FunctionPermissionFilterTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task InvokeAsync_WhenFunctionCodeIsEmpty_ShouldCallNext()
    {
        // Arrange
        var filter = new FunctionPermissionFilter(string.Empty);
        var httpContext = new DefaultHttpContext();
        var nextCalled = false;
        
        var context = CreateFilterContext(httpContext);
        EndpointFilterDelegate next = _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        // Act
        var result = await filter.InvokeAsync(context, next);

        // Assert
        nextCalled.Should().BeTrue();
        result.Should().BeOfType<Ok>();
    }

    [Fact]
    public async Task InvokeAsync_WhenUserIdMissing_ShouldReturnUnauthorized()
    {
        // Arrange
        var filter = new FunctionPermissionFilter("TEST.FUNCTION");
        var httpContext = new DefaultHttpContext();
        // No user claims set
        
        var context = CreateFilterContext(httpContext);
        EndpointFilterDelegate next = _ => ValueTask.FromResult<object?>(Results.Ok());

        // Act
        var result = await filter.InvokeAsync(context, next);

        // Assert
        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    // Note: Tests for HasFunctionAccessAsync require integration testing
    // because AccessService methods are not virtual and cannot be mocked.
    // These scenarios are covered in integration tests instead.

    [Fact]
    public async Task InvokeAsync_WhenUserIdIsWhitespace_ShouldReturnUnauthorized()
    {
        // Arrange
        var filter = new FunctionPermissionFilter("TEST.FUNCTION");
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "   ")
        }, "TestAuth"));
        
        var context = CreateFilterContext(httpContext);
        EndpointFilterDelegate next = _ => ValueTask.FromResult<object?>(Results.Ok());

        // Act
        var result = await filter.InvokeAsync(context, next);

        // Assert
        result.Should().BeOfType<UnauthorizedHttpResult>();
    }

    private static EndpointFilterInvocationContext CreateFilterContext(HttpContext httpContext)
    {
        return new DefaultEndpointFilterInvocationContext(httpContext);
    }
}

/// <summary>
/// Helper class for creating filter context
/// </summary>
internal class DefaultEndpointFilterInvocationContext : EndpointFilterInvocationContext
{
    public DefaultEndpointFilterInvocationContext(HttpContext httpContext)
    {
        HttpContext = httpContext;
    }

    public override HttpContext HttpContext { get; }
    public override IList<object?> Arguments => new List<object?>();

    public override T GetArgument<T>(int index) => default!;
}
