using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// AdminEndpoints 管理端点深度测试
/// 覆盖数据库操作、模板管理等
/// </summary>
public class AdminEndpointsCrudTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public AdminEndpointsCrudTests(TestWebAppFactory factory)
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

    #region Database Health Tests

    [Fact]
    public async Task GetDbHealth_WithAuth_ShouldReturnHealthStatus()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/admin/db/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Debug Users Tests (Development only)

    [Fact]
    public async Task GetDebugUsers_WithAuth_ShouldNotReturn401()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act - Debug endpoint under /api/admin/debug/users
        var response = await client.GetAsync("/api/admin/debug/users");

        // Assert - Should not be 401 (may be 404 if debug endpoints are disabled)
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Template Regeneration Tests

    [Fact]
    public async Task RegenerateDefaultTemplates_WithAuth_ShouldNotReturn401()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.PostAsync("/api/admin/templates/regenerate-defaults", null);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegenerateTemplateForEntity_WithAuth_ShouldNotReturn401()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.PostAsync("/api/admin/templates/customer/regenerate", null);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }


    #endregion

    #region Reset Password Tests

    [Fact]
    public async Task ResetPassword_WithAuth_ShouldNotReturn401()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var request = new
        {
            userName = "admin",
            newPassword = "NewPassword123!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/reset-password", request);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}
