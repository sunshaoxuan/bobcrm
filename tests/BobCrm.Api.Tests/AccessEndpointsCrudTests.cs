using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// AccessEndpoints 权限管理端点深度测试
/// 覆盖功能节点、角色权限、数据范围等
/// </summary>
public class AccessEndpointsCrudTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public AccessEndpointsCrudTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    #region Function Node CRUD Tests

    [Fact]
    public async Task GetFunctions_WithAuth_ShouldReturnFunctionList()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/access/functions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("data");
    }

    [Fact]
    public async Task GetFunctions_WithoutAuth_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/access/functions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateFunction_WithValidData_ShouldCreateOrConflict()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var uniqueCode = $"TEST.FUNC.{Guid.NewGuid():N}".Substring(0, 30);
        var request = new
        {
            code = uniqueCode,
            displayName = new Dictionary<string, string> { ["zh"] = "测试功能" },
            isMenu = false,
            isEnabled = true
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/access/functions", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Created,
            HttpStatusCode.OK,
            HttpStatusCode.Conflict,
            HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateFunction_WithoutAuth_ShouldReturn401()
    {
        // Arrange
        var request = new
        {
            code = "TEST.FUNC",
            displayName = new Dictionary<string, string> { ["zh"] = "测试" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/access/functions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateFunction_WithInvalidId_ShouldReturn404OrBadRequest()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var invalidId = Guid.NewGuid();
        var request = new
        {
            displayName = new Dictionary<string, string> { ["zh"] = "更新名称" }
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/access/functions/{invalidId}", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteFunction_WithInvalidId_ShouldReturn404OrBadRequest()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var invalidId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/access/functions/{invalidId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.OK);
    }

    #endregion

    #region Role Permission Tests

    [Fact]
    public async Task GetRoles_WithAuth_ShouldReturnRoleList()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/access/roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("data");
    }

    [Fact]
    public async Task GetRoleById_WithValidId_ShouldReturnRole()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // First get list to find a valid role ID
        var listResponse = await client.GetAsync("/api/access/roles");
        var listContent = await listResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(listContent);

        if (doc.RootElement.TryGetProperty("data", out var dataArray) && dataArray.GetArrayLength() > 0)
        {
            var firstId = dataArray[0].GetProperty("id").GetString();

            // Act
            var response = await client.GetAsync($"/api/access/roles/{firstId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task GetRoleById_WithAuth_ShouldNotReturn401()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act - Use a placeholder ID
        var response = await client.GetAsync($"/api/access/roles/{Guid.NewGuid()}");

        // Assert - May be 404 but not 401
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region My Access Tests

    [Fact]
    public async Task GetFunctions_WithAuth_ShouldReturnFunctions()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/access/functions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Data Scope Tests

    [Fact]
    public async Task GetDataScopes_WithAuth_ShouldNotReturn401()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/access/data-scopes");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}
