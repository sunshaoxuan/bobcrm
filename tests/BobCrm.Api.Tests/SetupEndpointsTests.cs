using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// SetupEndpoints 系统初始化端点测试
/// </summary>
public class SetupEndpointsTests : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client;

    public SetupEndpointsTests(TestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    #region GetAdminInfo Tests

    [Fact]
    public async Task GetAdminInfo_ShouldReturn200()
    {
        // Act
        var response = await _client.GetAsync("/api/setup/admin");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAdminInfo_ShouldReturnAdminInfoDto()
    {
        // Act
        var response = await _client.GetAsync("/api/setup/admin");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        content.Should().Contain("exists");
    }

    [Fact]
    public async Task GetAdminInfo_WhenAdminExists_ShouldReturnExistsTrue()
    {
        // Act
        var response = await _client.GetAsync("/api/setup/admin");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        // In test environment, admin should exist (seeded by TestWebAppFactory)
        content.Should().Contain("\"exists\":true");
    }

    #endregion

    #region SetupAdmin Tests

    [Fact]
    public async Task SetupAdmin_WithInvalidPayload_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new { };

        // Act
        var response = await _client.PostAsJsonAsync("/api/setup/admin", request);

        // Assert - Should fail validation
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnsupportedMediaType);
    }

    #endregion

    #region AllowAnonymous Tests

    [Fact]
    public async Task GetAdminInfo_ShouldBeAccessibleAnonymously()
    {
        // Act - Use a fresh client without auth
        var response = await _client.GetAsync("/api/setup/admin");

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}
