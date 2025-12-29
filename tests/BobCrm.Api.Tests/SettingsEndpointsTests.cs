using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// SettingsEndpoints 设置端点测试
/// </summary>
public class SettingsEndpointsTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public SettingsEndpointsTests(TestWebAppFactory factory)
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

    #region System Settings Tests

    [Fact]
    public async Task GetSystemSettings_WithoutAuth_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/settings/system");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSystemSettings_WithAdminAuth_ShouldNotReturn401()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/settings/system");

        // Assert - Admin should have access (not 401)
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region User Settings Tests

    [Fact]
    public async Task GetUserSettings_WithoutAuth_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/settings/user");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserSettings_WithAuth_ShouldReturn200()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/settings/user");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserSettings_ShouldReturnUserSettingsSnapshot()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/settings/user");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateUserSettings_WithAuth_ShouldReturn200()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();
        var request = new
        {
            theme = "dark",
            language = "zh"
        };

        // Act
        var response = await client.PutAsJsonAsync("/api/settings/user", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region SMTP Test Endpoint Tests

    [Fact]
    public async Task SendSmtpTestEmail_WithoutAuth_ShouldReturn401()
    {
        // Arrange
        var request = new { to = "test@test.com" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/settings/system/smtp/test", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
