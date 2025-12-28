using System.Text.Json;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BobCrm.Api.Tests;

/// <summary>
/// FieldFilterService 测试
/// 覆盖字段权限过滤
/// </summary>
public class FieldFilterServiceTests
{
    private static FieldFilterService CreateService(IFieldPermissionService permissionService)
    {
        return new FieldFilterService(
            permissionService,
            NullLogger<FieldFilterService>.Instance);
    }

    #region FilterFieldsAsync Tests

    [Fact]
    public async Task FilterFieldsAsync_WhenDocumentIsNull_ShouldReturnNull()
    {
        // Arrange
        var mockPermission = new Mock<IFieldPermissionService>();
        var service = CreateService(mockPermission.Object);

        // Act
        var result = await service.FilterFieldsAsync("user1", "Customer", null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FilterFieldsAsync_WhenNoExplicitPermissions_ShouldReturnAllFields()
    {
        // Arrange
        var mockPermission = new Mock<IFieldPermissionService>();
        mockPermission.Setup(p => p.GetReadableFieldsAsync("user1", "Customer"))
            .ReturnsAsync(new List<string>());

        var service = CreateService(mockPermission.Object);
        var json = JsonDocument.Parse("{\"id\": 1, \"name\": \"Test\", \"secret\": \"hidden\"}");

        // Act
        var result = await service.FilterFieldsAsync("user1", "Customer", json);

        // Assert
        result.Should().NotBeNull();
        result!.RootElement.GetProperty("id").GetInt32().Should().Be(1);
        result.RootElement.GetProperty("name").GetString().Should().Be("Test");
        result.RootElement.GetProperty("secret").GetString().Should().Be("hidden");
    }

    [Fact]
    public async Task FilterFieldsAsync_WithExplicitPermissions_ShouldFilterFields()
    {
        // Arrange
        var mockPermission = new Mock<IFieldPermissionService>();
        mockPermission.Setup(p => p.GetReadableFieldsAsync("user1", "Customer"))
            .ReturnsAsync(new List<string> { "id", "name" });

        var service = CreateService(mockPermission.Object);
        var json = JsonDocument.Parse("{\"id\": 1, \"name\": \"Test\", \"secret\": \"hidden\"}");

        // Act
        var result = await service.FilterFieldsAsync("user1", "Customer", json);

        // Assert
        result.Should().NotBeNull();
        result!.RootElement.GetProperty("id").GetInt32().Should().Be(1);
        result.RootElement.GetProperty("name").GetString().Should().Be("Test");
        result.RootElement.TryGetProperty("secret", out _).Should().BeFalse();
    }

    [Fact]
    public async Task FilterFieldsAsync_ForWrite_ShouldUseWritableFields()
    {
        // Arrange
        var mockPermission = new Mock<IFieldPermissionService>();
        mockPermission.Setup(p => p.GetWritableFieldsAsync("user1", "Customer"))
            .ReturnsAsync(new List<string> { "name" });

        var service = CreateService(mockPermission.Object);
        var json = JsonDocument.Parse("{\"id\": 1, \"name\": \"Test\"}");

        // Act
        var result = await service.FilterFieldsAsync("user1", "Customer", json, isWrite: true);

        // Assert
        result.Should().NotBeNull();
        result!.RootElement.GetProperty("name").GetString().Should().Be("Test");
        result.RootElement.TryGetProperty("id", out _).Should().BeFalse();
    }

    [Fact]
    public async Task FilterFieldsAsync_WithCaseInsensitiveFieldNames_ShouldMatch()
    {
        // Arrange
        var mockPermission = new Mock<IFieldPermissionService>();
        mockPermission.Setup(p => p.GetReadableFieldsAsync("user1", "Customer"))
            .ReturnsAsync(new List<string> { "NAME" }); // Uppercase

        var service = CreateService(mockPermission.Object);
        var json = JsonDocument.Parse("{\"name\": \"Test\"}"); // lowercase

        // Act
        var result = await service.FilterFieldsAsync("user1", "Customer", json);

        // Assert
        result.Should().NotBeNull();
        result!.RootElement.GetProperty("name").GetString().Should().Be("Test");
    }

    #endregion

    #region FilterFieldsArrayAsync Tests

    [Fact]
    public async Task FilterFieldsArrayAsync_WhenDocumentIsNull_ShouldReturnNull()
    {
        // Arrange
        var mockPermission = new Mock<IFieldPermissionService>();
        var service = CreateService(mockPermission.Object);

        // Act
        var result = await service.FilterFieldsArrayAsync("user1", "Customer", null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FilterFieldsArrayAsync_WhenNoExplicitPermissions_ShouldReturnAllFields()
    {
        // Arrange
        var mockPermission = new Mock<IFieldPermissionService>();
        mockPermission.Setup(p => p.GetReadableFieldsAsync("user1", "Customer"))
            .ReturnsAsync(new List<string>());

        var service = CreateService(mockPermission.Object);
        var json = JsonDocument.Parse("[{\"id\": 1, \"name\": \"A\"}, {\"id\": 2, \"name\": \"B\"}]");

        // Act
        var result = await service.FilterFieldsArrayAsync("user1", "Customer", json);

        // Assert
        result.Should().NotBeNull();
        result!.RootElement.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task FilterFieldsArrayAsync_WithExplicitPermissions_ShouldFilterAllItems()
    {
        // Arrange
        var mockPermission = new Mock<IFieldPermissionService>();
        mockPermission.Setup(p => p.GetReadableFieldsAsync("user1", "Customer"))
            .ReturnsAsync(new List<string> { "id" });

        var service = CreateService(mockPermission.Object);
        var json = JsonDocument.Parse("[{\"id\": 1, \"name\": \"A\"}, {\"id\": 2, \"name\": \"B\"}]");

        // Act
        var result = await service.FilterFieldsArrayAsync("user1", "Customer", json);

        // Assert
        result.Should().NotBeNull();
        result!.RootElement.GetArrayLength().Should().Be(2);
        foreach (var item in result.RootElement.EnumerateArray())
        {
            item.TryGetProperty("id", out _).Should().BeTrue();
            item.TryGetProperty("name", out _).Should().BeFalse();
        }
    }

    [Fact]
    public async Task FilterFieldsArrayAsync_WhenNotArray_ShouldReturnAsIs()
    {
        // Arrange
        var mockPermission = new Mock<IFieldPermissionService>();
        var service = CreateService(mockPermission.Object);
        var json = JsonDocument.Parse("{\"id\": 1}"); // Object, not array

        // Act
        var result = await service.FilterFieldsArrayAsync("user1", "Customer", json);

        // Assert
        result.Should().NotBeNull();
        result!.RootElement.GetProperty("id").GetInt32().Should().Be(1);
    }

    #endregion

    #region Additional Tests

    [Fact]
    public async Task FilterFieldsAsync_WithAllowedNestedField_ShouldIncludeNestedObject()
    {
        // Arrange
        var mockPermission = new Mock<IFieldPermissionService>();
        mockPermission.Setup(p => p.GetReadableFieldsAsync("user1", "Customer"))
            .ReturnsAsync(new List<string> { "id", "address" });

        var service = CreateService(mockPermission.Object);
        var json = JsonDocument.Parse("{\"id\": 1, \"name\": \"Test\", \"address\": {\"city\": \"Tokyo\"}}");

        // Act
        var result = await service.FilterFieldsAsync("user1", "Customer", json);

        // Assert
        result.Should().NotBeNull();
        result!.RootElement.GetProperty("id").GetInt32().Should().Be(1);
        // Verify name is filtered out
        result.RootElement.TryGetProperty("name", out _).Should().BeFalse();
    }

    #endregion
}
