using BobCrm.Api.Abstractions;
using BobCrm.Api.Services.DataSources;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// EntityDataSourceHandler 实体数据源处理器测试
/// </summary>
public class EntityDataSourceHandlerTests
{
    private readonly Mock<ILogger<EntityDataSourceHandler>> _mockLogger;

    public EntityDataSourceHandlerTests()
    {
        _mockLogger = new Mock<ILogger<EntityDataSourceHandler>>();
    }

    private EntityDataSourceHandler CreateHandler()
    {
        return new EntityDataSourceHandler(_mockLogger.Object);
    }

    #region TypeCode Tests

    [Fact]
    public void TypeCode_ShouldBeEntity()
    {
        // Arrange
        var handler = CreateHandler();

        // Assert
        handler.TypeCode.Should().Be("entity");
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_ShouldReturnResult()
    {
        // Arrange
        var handler = CreateHandler();
        var request = new DataSourceExecutionRequest
        {
            TypeCode = "entity",
            ConfigJson = JsonSerializer.Serialize(new { EntityType = "Customer" }),
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnDataJson()
    {
        // Arrange
        var handler = CreateHandler();
        var request = new DataSourceExecutionRequest
        {
            TypeCode = "entity",
            ConfigJson = JsonSerializer.Serialize(new { EntityType = "Customer" }),
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.DataJson.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIncludeExecutionTime()
    {
        // Arrange
        var handler = CreateHandler();
        var request = new DataSourceExecutionRequest
        {
            TypeCode = "entity",
            ConfigJson = JsonSerializer.Serialize(new { EntityType = "Customer" }),
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.ExecutionTimeMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldIncludeAppliedScopes()
    {
        // Arrange
        var handler = CreateHandler();
        var request = new DataSourceExecutionRequest
        {
            TypeCode = "entity",
            ConfigJson = JsonSerializer.Serialize(new { EntityType = "Customer" }),
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await handler.ExecuteAsync(request);

        // Assert
        result.AppliedScopes.Should().NotBeNull();
    }

    #endregion

    #region ValidateConfigAsync Tests

    [Fact]
    public async Task ValidateConfigAsync_WithValidConfig_ShouldReturnSuccess()
    {
        // Arrange
        var handler = CreateHandler();
        var configJson = JsonSerializer.Serialize(new { EntityType = "Customer" });

        // Act
        var result = await handler.ValidateConfigAsync(configJson);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateConfigAsync_WithEmptyEntityType_ShouldReturnFailure()
    {
        // Arrange
        var handler = CreateHandler();
        var configJson = JsonSerializer.Serialize(new { EntityType = "" });

        // Act
        var result = await handler.ValidateConfigAsync(configJson);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("实体类型不能为空"));
    }

    [Fact]
    public async Task ValidateConfigAsync_WithNullEntityType_ShouldReturnFailure()
    {
        // Arrange
        var handler = CreateHandler();
        var configJson = JsonSerializer.Serialize(new { EntityType = (string?)null });

        // Act
        var result = await handler.ValidateConfigAsync(configJson);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateConfigAsync_WithInvalidJson_ShouldReturnFailure()
    {
        // Arrange
        var handler = CreateHandler();
        var invalidJson = "{ invalid json }";

        // Act
        var result = await handler.ValidateConfigAsync(invalidJson);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("JSON格式错误"));
    }

    #endregion

    #region GetFieldsAsync Tests

    [Fact]
    public async Task GetFieldsAsync_ShouldReturnEmptyList()
    {
        // Arrange
        var handler = CreateHandler();
        var configJson = JsonSerializer.Serialize(new { EntityType = "Customer" });

        // Act
        var result = await handler.GetFieldsAsync(configJson);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty(); // Currently returns mock empty list
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task ExecuteAsync_ShouldLogExecution()
    {
        // Arrange
        var handler = CreateHandler();
        var request = new DataSourceExecutionRequest
        {
            TypeCode = "entity",
            ConfigJson = JsonSerializer.Serialize(new { EntityType = "Customer" }),
            Page = 1,
            PageSize = 10
        };

        // Act
        await handler.ExecuteAsync(request);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("执行实体数据源查询")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
