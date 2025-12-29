using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// AccessEndpoints 权限管理端点扩展测试
/// </summary>
public class AccessEndpointsExtendedTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public AccessEndpointsExtendedTests(TestWebAppFactory factory)
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
    public async Task GetFunctions_WithoutAuth_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/access/functions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetFunctions_WithAuth_ShouldReturn200()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/access/functions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetFunctionTree_WithAuth_ShouldNotReturn401()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/access/function-tree");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Role Permission Tests

    [Fact]
    public async Task GetRoles_WithAuth_ShouldNotReturn401()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/access/roles");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region My Functions Tests

    [Fact]
    public async Task GetMyFunctions_WithAuth_ShouldNotReturn401()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/access/my");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region CreateFunction Tests

    [Fact]
    public async Task CreateFunction_WithoutAuth_ShouldReturn401()
    {
        // Arrange
        var request = new
        {
            code = "TEST.FUNCTION",
            displayName = new { zh = "测试功能" },
            isMenu = false
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/access/functions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
