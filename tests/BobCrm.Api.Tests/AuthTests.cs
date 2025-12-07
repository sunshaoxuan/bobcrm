using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BobCrm.Api.Tests;

public class AuthTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    public AuthTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_Refresh_Session_Ok()
    {
        var client = _factory.CreateClient();

        var (access, refresh) = await client.LoginAsAdminAsync();

        // session with bearer
        client.UseBearer(access);
        var session = await client.GetAsync("/api/auth/session");
        session.EnsureSuccessStatusCode();
        var payload = (await session.ReadAsJsonAsync()).UnwrapData();
        Assert.True(payload.GetProperty("valid").GetBoolean());

        // refresh
        var r = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refresh });
        r.EnsureSuccessStatusCode();
        var refreshed = (await r.ReadAsJsonAsync()).UnwrapData();
        var access2 = refreshed.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(access2));

        // logout
        var lo = await client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = refresh });
        Assert.Equal(HttpStatusCode.OK, lo.StatusCode);
    }

    [Fact]
    public async Task Login_Response_Should_Be_Wrapped_In_Data()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/auth/login", new { username = "admin", password = "Admin@12345" });
        resp.EnsureSuccessStatusCode();

        var root = await resp.ReadAsJsonAsync();
        Assert.True(root.TryGetProperty("data", out var data), "Login response should contain data wrapper");
        Assert.Equal(JsonValueKind.Object, data.ValueKind);
        Assert.True(data.TryGetProperty("accessToken", out _), "accessToken should exist inside data");
        Assert.True(data.TryGetProperty("refreshToken", out _), "refreshToken should exist inside data");
    }
}

