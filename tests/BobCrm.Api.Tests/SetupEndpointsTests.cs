using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BobCrm.Api.Tests;

/// <summary>
/// SetupEndpoints 系统初始化端点测试
/// </summary>
public class SetupEndpointsTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public SetupEndpointsTests(TestWebAppFactory factory)
    {
        _factory = factory;
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

    [Fact]
    public async Task SetupAdmin_WhenPasswordTooWeak_ShouldReturnBadRequestWithCode()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var admin = await um.FindByNameAsync("admin");
            admin.Should().NotBeNull();
            await um.DeleteAsync(admin!);
        }

        var response = await client.PostAsJsonAsync("/api/setup/admin", new
        {
            username = "admin",
            email = "admin@local",
            password = "123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var root = await response.ReadAsJsonAsync();
        root.GetProperty("code").GetString().Should().Be("SETUP_CREATE_FAILED");
    }

    [Fact]
    public async Task SetupAdmin_WhenExistingAdminCustomizedAndDefaultPasswordInvalid_ShouldReturn403()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var admin = await um.FindByNameAsync("admin");
            admin.Should().NotBeNull();

            admin!.Email = "custom@local";
            await um.UpdateAsync(admin);

            await um.RemovePasswordAsync(admin);
            await um.AddPasswordAsync(admin, "NewAdmin@12345");
        }

        var response = await client.PostAsJsonAsync("/api/setup/admin", new
        {
            username = "newadmin",
            email = "newadmin@local",
            password = "NewAdmin@12345"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var root = await response.ReadAsJsonAsync();
        root.GetProperty("code").GetString().Should().Be("FORBIDDEN");
    }

    [Fact]
    public async Task SetupAdmin_WhenDefaultAdmin_ShouldUpdateSuccessfully()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/setup/admin", new
        {
            username = "admin2",
            email = "admin2@local",
            password = "Admin2@12345"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

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
