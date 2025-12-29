using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// TemplateEndpoints 模板端点扩展测试
/// </summary>
public class TemplateEndpointsExtendedTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public TemplateEndpointsExtendedTests(TestWebAppFactory factory)
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

    #region Template CRUD Tests

    [Fact]
    public async Task CreateTemplate_WithoutAuth_ShouldReturn401()
    {
        // Arrange
        var request = new
        {
            name = "Test Template",
            entityType = "Customer",
            layoutJson = "{}"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/templates", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTemplateById_WithAuth_ShouldNotReturn401()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/templates/1");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Template Query Tests

    [Fact]
    public async Task GetTemplatesByEntityType_WithAuth_ShouldNotReturn401()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/templates?entityType=customer");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Default Template Tests

    [Fact]
    public async Task GetDefaultTemplate_WithAuth_ShouldNotReturn401()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/templates/default/customer");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}
