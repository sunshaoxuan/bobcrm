using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BobCrm.Api.Contracts.Requests.Auth;
using FluentAssertions;
using Xunit;

namespace BobCrm.Api.Tests;

public class AuthEndpointsFinalSprintTests
{
    [Fact]
    public async Task Login_WithEmptyUsername_ShouldReturn400()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("", "x"));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturn401()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("admin", "Wrong@12345"));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Activate_WithMissingUser_ShouldReturn404()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        var resp = await client.GetAsync($"/api/auth/activate?userId={Guid.NewGuid():N}&code=x");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ShouldReturn401()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshRequest("invalid-refresh-token"));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithoutAuth_ShouldReturn401()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/auth/logout", new LogoutRequest(""));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Session_WithAuth_ShouldReturnValidTrue()
    {
        using var factory = new TestWebAppFactory();
        var client = factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var resp = await client.GetAsync("/api/auth/session");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await resp.ReadDataAsJsonAsync();
        data.GetProperty("valid").GetBoolean().Should().BeTrue();
    }
}
