using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace BobCrm.Api.Tests;

public class AuthEndpointsTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public AuthEndpointsTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WhenUsernameMissing_ShouldReturn400()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login", new { username = "", password = "x" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var root = await resp.ReadAsJsonAsync();
        Assert.Equal("INVALID_REQUEST", GetErrorCode(root));
    }

    [Fact]
    public async Task Login_WhenUserNotFound_ShouldReturn401()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login", new { username = "no_such_user", password = "x" });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        var root = await resp.ReadAsJsonAsync();
        Assert.Equal("AUTH_FAILED", GetErrorCode(root));
    }

    [Fact]
    public async Task Login_WhenEmailNotConfirmed_ShouldReturn400()
    {
        var client = _factory.CreateClient();
        var username = $"user_{Guid.NewGuid():N}";
        var email = $"{username}@local";
        var password = "User@12345";

        var reg = await client.PostAsJsonAsync("/api/auth/register", new { username, password, email });
        reg.EnsureSuccessStatusCode();

        var login = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);
        var root = await login.ReadAsJsonAsync();
        Assert.Equal("EMAIL_NOT_CONFIRMED", GetErrorCode(root));
    }

    [Fact]
    public async Task Refresh_WhenTokenUsedTwice_ShouldRejectSecondAttempt()
    {
        var client = _factory.CreateClient();
        var (accessToken, refreshToken) = await client.LoginAsAdminAsync();

        var first = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        var root = await second.ReadAsJsonAsync();
        Assert.Equal("AUTH_FAILED", GetErrorCode(root));
    }

    [Fact]
    public async Task Refresh_WhenTokenInvalid_ShouldReturn401()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = "invalid" });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
        var root = await resp.ReadAsJsonAsync();
        Assert.Equal("AUTH_FAILED", GetErrorCode(root));
    }

    private static string? GetErrorCode(JsonElement root)
    {
        if (root.TryGetProperty("code", out var code))
        {
            return code.GetString();
        }

        if (root.TryGetProperty("Code", out var codePascal))
        {
            return codePascal.GetString();
        }

        return null;
    }
}
