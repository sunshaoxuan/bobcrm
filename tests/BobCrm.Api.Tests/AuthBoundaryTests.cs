using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace BobCrm.Api.Tests;

/// <summary>
/// 认证边界和错误场景测试
/// </summary>
public class AuthBoundaryTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    public AuthBoundaryTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_Fails_When_Email_Not_Confirmed()
    {
        var client = _factory.CreateClient();
        var username = $"user_{Guid.NewGuid():N}";
        var email = $"{username}@local";
        var password = "User@12345";

        // 注册用户但不激活
        var reg = await client.PostAsJsonAsync("/api/auth/register", new { username, password, email });
        reg.EnsureSuccessStatusCode();

        // 尝试登录 - 应该返回 400（邮箱未确认）
        var login = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);

        var errorContent = await login.Content.ReadAsStringAsync();
        var errorJson = JsonDocument.Parse(errorContent).RootElement;
        Assert.True(errorJson.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task Login_Fails_With_Wrong_Password()
    {
        var client = _factory.CreateClient();
        var username = $"user_{Guid.NewGuid():N}";
        var email = $"{username}@local";
        var password = "User@12345";

        // 注册并激活用户
        var reg = await client.PostAsJsonAsync("/api/auth/register", new { username, password, email });
        reg.EnsureSuccessStatusCode();

        using (var scope = _factory.Services.CreateScope())
        {
            var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var u = await um.FindByNameAsync(username);
            var code = await um.GenerateEmailConfirmationTokenAsync(u!);
            var act = await client.GetAsync($"/api/auth/activate?userId={Uri.EscapeDataString(u!.Id)}&code={Uri.EscapeDataString(code)}");
            act.EnsureSuccessStatusCode();
        }

        // 使用错误的密码登录 - 应该返回 401
        var login = await client.PostAsJsonAsync("/api/auth/login", new { username, password = "WrongPassword@123" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Login_Fails_With_Nonexistent_Username()
    {
        var client = _factory.CreateClient();

        // 使用不存在的用户名登录 - 应该返回 401
        var login = await client.PostAsJsonAsync("/api/auth/login", 
            new { username = "nonexistent_user_12345", password = "AnyPassword@123" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);

        var errorContent = await login.Content.ReadAsStringAsync();
        var errorJson = JsonDocument.Parse(errorContent).RootElement;
        Assert.True(errorJson.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task Refresh_Token_Works_When_Valid()
    {
        var client = _factory.CreateClient();
        var (accessToken, refreshToken) = await client.LoginAsAdminAsync();

        // 等待一小段时间，确保新JWT的时间戳不同
        await Task.Delay(1000);

        // 使用有效的刷新令牌应该成功
        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });
        refresh.EnsureSuccessStatusCode();

        var json = JsonDocument.Parse(await refresh.Content.ReadAsStringAsync()).RootElement;
        Assert.True(json.TryGetProperty("accessToken", out var newAccess));
        Assert.True(json.TryGetProperty("refreshToken", out var newRefresh));
        // 新令牌应该不同
        Assert.NotEqual(refreshToken, newRefresh.GetString());
        // AccessToken 可能相同（如果时间戳相同），但我们验证格式正确即可
        Assert.False(string.IsNullOrEmpty(newAccess.GetString()));
    }

    [Fact]
    public async Task Refresh_Token_Fails_When_Invalid()
    {
        var client = _factory.CreateClient();

        // 使用无效的刷新令牌应该返回 401
        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", 
            new { refreshToken = "invalid_token_12345" });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Refresh_Token_Fails_After_Revocation()
    {
        var client = _factory.CreateClient();
        var (accessToken, refreshToken) = await client.LoginAsAdminAsync();

        // 登出（撤销令牌）
        client.UseBearer(accessToken);
        var logout = await client.PostAsJsonAsync("/api/auth/logout", new { refreshToken });
        logout.EnsureSuccessStatusCode();

        // 尝试使用已撤销的令牌刷新 - 应该失败
        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Login_Empty_Username_Returns_BadRequest()
    {
        var client = _factory.CreateClient();

        // 空用户名应该返回 400
        var login = await client.PostAsJsonAsync("/api/auth/login", 
            new { username = "", password = "AnyPassword@123" });
        Assert.Equal(HttpStatusCode.BadRequest, login.StatusCode);
    }

    [Fact]
    public async Task Session_Endpoint_Returns_Valid_For_Authenticated_User()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var session = await client.GetFromJsonAsync<JsonElement>("/api/auth/session");
        Assert.True(session.GetProperty("valid").GetBoolean());
        Assert.True(session.TryGetProperty("user", out var user));
        Assert.True(user.TryGetProperty("username", out _));
    }

    [Fact]
    public async Task Session_Endpoint_Requires_Auth()
    {
        var client = _factory.CreateClient();

        // 未认证访问 session 端点应该返回 401
        var response = await client.GetAsync("/api/auth/session");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_By_Email_Also_Works()
    {
        var client = _factory.CreateClient();
        var username = $"user_{Guid.NewGuid():N}";
        var email = $"{username}@local";
        var password = "User@12345";

        // 注册并激活
        var reg = await client.PostAsJsonAsync("/api/auth/register", new { username, password, email });
        reg.EnsureSuccessStatusCode();

        using (var scope = _factory.Services.CreateScope())
        {
            var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var u = await um.FindByNameAsync(username);
            u!.EmailConfirmed = true;
            await um.UpdateAsync(u);
        }

        // 使用邮箱登录（而不是用户名）
        var loginByEmail = await client.PostAsJsonAsync("/api/auth/login", new { username = email, password });
        loginByEmail.EnsureSuccessStatusCode();

        var json = JsonDocument.Parse(await loginByEmail.Content.ReadAsStringAsync()).RootElement;
        Assert.True(json.TryGetProperty("accessToken", out _));
    }
}

