using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// EntityAggregateEndpoints 实体聚合端点测试
/// </summary>
public class EntityAggregateEndpointsTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public EntityAggregateEndpointsTests(TestWebAppFactory factory)
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

    #region Entity Definitions with SubEntities Tests

    [Fact]
    public async Task GetEntityDefinitions_WithAuth_ShouldReturn200()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/entity-definitions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetEntityDefinitions_WithoutAuth_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/entity-definitions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Query Tests

    [Fact]
    public async Task GetEntityDefinitionById_WithAuth_ShouldNotReturn401()
    {
        // Arrange
        var client = await GetAuthenticatedClientAsync();

        // Act
        var response = await client.GetAsync("/api/entity-definitions/" + Guid.NewGuid());

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}
