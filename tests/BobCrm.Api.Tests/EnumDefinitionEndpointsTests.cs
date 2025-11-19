using System.Net;
using System.Net.Http.Json;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// EnumDefinition API Endpoints 集成测试
/// 测试 HTTP 端点行为
/// </summary>
public class EnumDefinitionEndpointsTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public EnumDefinitionEndpointsTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.UseBearer(accessToken);
        return client;
    }

    [Fact]
    public async Task GetAllEnums_ReturnsOk_WithEnumList()
    {
        // Arrange
        await SeedTestEnumAsync();
        var client = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/enums");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var enums = await response.Content.ReadFromJsonAsync<List<EnumDefinitionDto>>();
        Assert.NotNull(enums);
        Assert.NotEmpty(enums);
    }

    [Fact]
    public async Task GetEnumById_ReturnsOk_WhenExists()
    {
        // Arrange
        var enumId = await SeedTestEnumAsync();
        var client = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync($"/api/enums/{enumId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<EnumDefinitionDto>();
        Assert.NotNull(result);
        Assert.Equal(enumId, result.Id);
    }

    [Fact]
    public async Task GetEnumById_ReturnsNotFound_WhenNotExists()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync($"/api/enums/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetEnumByCode_ReturnsOk_WhenExists()
    {
        // Arrange
        var code = $"test_enum_by_code_{Guid.NewGuid():N}";
        await SeedTestEnumAsync(code: code);
        var client = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync($"/api/enums/by-code/{code}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<EnumDefinitionDto>();
        Assert.NotNull(result);
        Assert.Equal(code, result.Code);
    }

    [Fact]
    public async Task CreateEnum_ReturnsCreated_WithValidRequest()
    {
        // Arrange
        var client = await CreateAuthenticatedClientAsync();
        var uniqueCode = $"new_test_enum_{Guid.NewGuid():N}";
        var request = new CreateEnumDefinitionRequest
        {
            Code = uniqueCode,
            DisplayName = new() { { "zh", "新测试枚举" }, { "en", "New Test Enum" } },
            Description = new() { { "zh", "测试描述" } },
            Options = new List<CreateEnumOptionRequest>
            {
                new() { Value = "OPT1", DisplayName = new() { { "zh", "选项1" } }, SortOrder = 0 },
                new() { Value = "OPT2", DisplayName = new() { { "zh", "选项2" } }, SortOrder = 1 }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/enums", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<EnumDefinitionDto>();
        Assert.NotNull(result);
        Assert.Equal(uniqueCode, result.Code);
        Assert.Equal(2, result.Options.Count);
    }

    [Fact]
    public async Task CreateEnum_ReturnsBadRequest_WhenCodeExists()
    {
        // Arrange
        var code = $"test_enum_duplicate_{Guid.NewGuid():N}";
        await SeedTestEnumAsync(code: code);
        var client = await CreateAuthenticatedClientAsync();
        var request = new CreateEnumDefinitionRequest
        {
            Code = code, // 已存在
            DisplayName = new() { { "zh", "重复" } }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/enums", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEnum_ReturnsOk_WithValidRequest()
    {
        // Arrange
        var enumId = await SeedTestEnumAsync(isSystem: false);
        var client = await CreateAuthenticatedClientAsync();
        var request = new UpdateEnumDefinitionRequest
        {
            DisplayName = new() { { "zh", "更新后名称" } },
            Description = new() { { "zh", "更新后描述" } },
            IsEnabled = false
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/enums/{enumId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<EnumDefinitionDto>();
        Assert.NotNull(result);
        Assert.Equal("更新后名称", result.DisplayName["zh"]);
        Assert.False(result.IsEnabled);
    }

    [Fact]
    public async Task UpdateEnum_ReturnsBadRequest_ForSystemEnum()
    {
        // Arrange
        var enumId = await SeedTestEnumAsync(isSystem: true);
        var client = await CreateAuthenticatedClientAsync();
        var request = new UpdateEnumDefinitionRequest
        {
            DisplayName = new() { { "zh", "尝试修改" } }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/enums/{enumId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEnum_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        var enumId = await SeedTestEnumAsync(isSystem: false);
        var client = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.DeleteAsync($"/api/enums/{enumId}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEnum_ReturnsBadRequest_ForSystemEnum()
    {
        // Arrange
        var enumId = await SeedTestEnumAsync(isSystem: true);
        var client = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.DeleteAsync($"/api/enums/{enumId}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEnumOptions_ReturnsOk_WithOptionsList()
    {
        // Arrange
        var enumId = await SeedTestEnumWithOptionsAsync();
        var client = await CreateAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync($"/api/enums/{enumId}/options");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var options = await response.Content.ReadFromJsonAsync<List<EnumOptionDto>>();
        Assert.NotNull(options);
        Assert.NotEmpty(options);
    }

    #region Helper Methods

    private async Task<Guid> SeedTestEnumAsync(bool isSystem = false, string? code = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var enumDef = new EnumDefinition
        {
            Code = code ?? $"test_enum_{Guid.NewGuid():N}",
            DisplayName = new() { { "zh", "测试枚举" }, { "en", "Test Enum" } },
            Description = new() { { "zh", "测试用" } },
            IsSystem = isSystem,
            IsEnabled = true
        };

        db.EnumDefinitions.Add(enumDef);
        await db.SaveChangesAsync();
        return enumDef.Id;
    }

    private async Task<Guid> SeedTestEnumWithOptionsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var enumDef = new EnumDefinition
        {
            Code = $"test_with_opts_{Guid.NewGuid():N}",
            DisplayName = new() { { "zh", "带选项" } },
            IsSystem = false,
            IsEnabled = true,
            Options = new List<EnumOption>
            {
                new() { Value = "VAL1", DisplayName = new() { { "zh", "值1" } }, SortOrder = 0, IsEnabled = true },
                new() { Value = "VAL2", DisplayName = new() { { "zh", "值2" } }, SortOrder = 1, IsEnabled = true }
            }
        };

        db.EnumDefinitions.Add(enumDef);
        await db.SaveChangesAsync();
        return enumDef.Id;
    }

    #endregion
}
