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
        var payload = JsonDocument.Parse(await session.Content.ReadAsStringAsync()).RootElement;
        Assert.True(payload.GetProperty("valid").GetBoolean());

        // refresh
        var r = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refresh });
        r.EnsureSuccessStatusCode();
        var refreshed = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
        var access2 = refreshed.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(access2));

        // logout
        var lo = await client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = refresh });
        Assert.Equal(HttpStatusCode.OK, lo.StatusCode);
    }
}

