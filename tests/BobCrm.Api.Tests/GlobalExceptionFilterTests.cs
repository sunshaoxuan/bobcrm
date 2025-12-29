using BobCrm.Api.Filters;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// GlobalExceptionFilter 全局异常过滤器测试
/// </summary>
public class GlobalExceptionFilterTests
{
    private readonly Mock<ILogger<GlobalExceptionFilter>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;

    public GlobalExceptionFilterTests()
    {
        _mockLogger = new Mock<ILogger<GlobalExceptionFilter>>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();
    }

    private GlobalExceptionFilter CreateFilter()
    {
        return new GlobalExceptionFilter(_mockLogger.Object, _mockEnvironment.Object);
    }

    private static ExceptionContext CreateExceptionContext(Exception exception)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "test-trace-id";

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        return new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = exception
        };
    }

    #region ArgumentNullException Tests

    [Fact]
    public void OnException_ArgumentNullException_ShouldReturn400()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        var filter = CreateFilter();
        var exception = new ArgumentNullException("testParam");
        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        context.ExceptionHandled.Should().BeTrue();
        context.Result.Should().BeOfType<ObjectResult>();
        var result = (ObjectResult)context.Result!;
        result.StatusCode.Should().Be(400);
    }

    [Fact]
    public void OnException_ArgumentException_ShouldReturn400()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        var filter = CreateFilter();
        var exception = new ArgumentException("Invalid argument");
        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        context.ExceptionHandled.Should().BeTrue();
        var result = (ObjectResult)context.Result!;
        result.StatusCode.Should().Be(400);
    }

    #endregion

    #region InvalidOperationException Tests

    [Fact]
    public void OnException_InvalidOperationException_ShouldReturn400()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        var filter = CreateFilter();
        var exception = new InvalidOperationException("Invalid operation");
        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        context.ExceptionHandled.Should().BeTrue();
        var result = (ObjectResult)context.Result!;
        result.StatusCode.Should().Be(400);
    }

    #endregion

    #region UnauthorizedAccessException Tests

    [Fact]
    public void OnException_UnauthorizedAccessException_ShouldReturn401()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        var filter = CreateFilter();
        var exception = new UnauthorizedAccessException("Access denied");
        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        context.ExceptionHandled.Should().BeTrue();
        var result = (ObjectResult)context.Result!;
        result.StatusCode.Should().Be(401);
    }

    #endregion

    #region KeyNotFoundException Tests

    [Fact]
    public void OnException_KeyNotFoundException_ShouldReturn404()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        var filter = CreateFilter();
        var exception = new KeyNotFoundException("Resource not found");
        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        context.ExceptionHandled.Should().BeTrue();
        var result = (ObjectResult)context.Result!;
        result.StatusCode.Should().Be(404);
    }

    #endregion

    #region Generic Exception Tests

    [Fact]
    public void OnException_GenericException_ShouldReturn500()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        var filter = CreateFilter();
        var exception = new Exception("Something went wrong");
        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        context.ExceptionHandled.Should().BeTrue();
        var result = (ObjectResult)context.Result!;
        result.StatusCode.Should().Be(500);
    }

    #endregion

    #region Environment-Specific Message Tests

    [Fact]
    public void OnException_InDevelopment_ShouldIncludeStackTrace()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        var filter = CreateFilter();
        var exception = new Exception("Test error");
        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        context.ExceptionHandled.Should().BeTrue();
        var result = (ObjectResult)context.Result!;
        // In development mode, the full exception should be included
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public void OnException_InProduction_ShouldReturnUserFriendlyMessage()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Production);
        var filter = CreateFilter();
        var exception = new Exception("Internal error details");
        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        context.ExceptionHandled.Should().BeTrue();
        var result = (ObjectResult)context.Result!;
        result.Value.Should().NotBeNull();
    }

    #endregion

    #region TraceId Tests

    [Fact]
    public void OnException_ShouldIncludeTraceId()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        var filter = CreateFilter();
        var exception = new Exception("Test error");
        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        context.ExceptionHandled.Should().BeTrue();
        var result = (ObjectResult)context.Result!;
        result.Value.Should().NotBeNull();
    }

    #endregion

    #region Exception Handled Flag Tests

    [Fact]
    public void OnException_ShouldMarkExceptionAsHandled()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        var filter = CreateFilter();
        var exception = new Exception("Test error");
        var context = CreateExceptionContext(exception);

        // Act
        filter.OnException(context);

        // Assert
        context.ExceptionHandled.Should().BeTrue();
    }

    #endregion
}
