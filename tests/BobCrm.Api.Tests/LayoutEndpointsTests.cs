using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// LayoutEndpoints 布局端点测试
/// </summary>
public class LayoutEndpointsTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public LayoutEndpointsTests(TestWebAppFactory factory)
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

    #region Authorization Tests

    [Fact]
    public async Task GetTemplates_WithoutAuth_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/templates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetTemplates Tests

    [Fact]
    public async Task GetTemplates_WithAuth_ShouldReturn200()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/templates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
