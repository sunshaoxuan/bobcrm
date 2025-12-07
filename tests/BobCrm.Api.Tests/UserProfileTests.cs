using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;

namespace BobCrm.Api.Tests;

/// <summary>
/// 用户个人资料相关端点测试
/// 测试 GET /api/auth/me 和 POST /api/auth/change-password
/// </summary>
public class UserProfileTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    public UserProfileTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetMe_Requires_Authentication()
    {
        var client = _factory.CreateClient();
        
        // 未认证访问 /api/auth/me 应该返回 401
        var resp = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task GetMe_Returns_User_Information()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        // 获取当前用户信息
        var resp = await client.GetAsync("/api/auth/me");
        resp.EnsureSuccessStatusCode();

        var userInfo = (await resp.ReadAsJsonAsync()).UnwrapData();
        
        // 验证返回的字段
        Assert.True(userInfo.TryGetProperty("id", out _), "应该包含id");
        Assert.True(userInfo.TryGetProperty("userName", out var userName), "应该包含userName");
        Assert.True(userInfo.TryGetProperty("email", out var email), "应该包含email");
        Assert.True(userInfo.TryGetProperty("role", out var role), "应该包含role");
        
        // 验证admin用户的信息
        Assert.Equal("admin", userName.GetString());
        Assert.Equal("admin@local", email.GetString());
        Assert.Equal("admin", role.GetString());
    }

    [Fact]
    public async Task GetMe_Regular_User_Returns_User_Role()
    {
        var client = _factory.CreateClient();
        var (userId, userName, access) = await client.CreateAndLoginUserAsync(_factory.Services);
        client.UseBearer(access);

        // 获取普通用户信息
        var resp = await client.GetAsync("/api/auth/me");
        resp.EnsureSuccessStatusCode();

        var userInfo = (await resp.ReadAsJsonAsync()).UnwrapData();
        
        Assert.Equal(userName, userInfo.GetProperty("userName").GetString());
        Assert.Equal("User", userInfo.GetProperty("role").GetString());
    }

    [Fact]
    public async Task ChangePassword_Requires_Authentication()
    {
        var client = _factory.CreateClient();
        
        // 未认证访问应该返回 401
        var resp = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = "old",
            newPassword = "new123456"
        });
        
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_With_Correct_Current_Password_Succeeds()
    {
        var client = _factory.CreateClient();
        
        // 创建一个新用户用于测试密码修改
        var (userId, userName, access) = await client.CreateAndLoginUserAsync(_factory.Services);
        client.UseBearer(access);

        // 修改密码（新密码需要符合ASP.NET Identity策略：至少6位，包含大小写字母和数字）
        var changeResp = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = "User@12345",  // CreateAndLoginUserAsync使用的默认密码
            newPassword = "NewPass@123456"   // 更强的密码
        });
        
        // 如果修改失败，打印错误信息
        if (!changeResp.IsSuccessStatusCode)
        {
            var error = await changeResp.Content.ReadAsStringAsync();
            throw new Exception($"密码修改失败: {error}");
        }
        
        changeResp.EnsureSuccessStatusCode();
        var result = await changeResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.TryGetProperty("message", out _), "应该返回成功消息");

        // 验证新密码可以登录
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = userName,
            password = "NewPass@123456"
        });
        
        loginResp.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ChangePassword_With_Wrong_Current_Password_Fails()
    {
        var client = _factory.CreateClient();
        var (userId, userName, access) = await client.CreateAndLoginUserAsync(_factory.Services);
        client.UseBearer(access);

        // 使用错误的当前密码
        var changeResp = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = "WrongPassword",
            newPassword = "NewPass@123456"
        });
        
        // 应该返回 400 Bad Request
        Assert.Equal(HttpStatusCode.BadRequest, changeResp.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_Validates_New_Password_Strength()
    {
        var client = _factory.CreateClient();
        var (userId, userName, access) = await client.CreateAndLoginUserAsync(_factory.Services);
        client.UseBearer(access);

        // 使用弱密码（太短）
        var changeResp = await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = "User@12345",
            newPassword = "123"  // 太短
        });
        
        // 应该返回 400 Bad Request（ASP.NET Identity密码策略）
        Assert.Equal(HttpStatusCode.BadRequest, changeResp.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_After_Success_Old_Password_Invalid()
    {
        var client = _factory.CreateClient();
        var (userId, userName, access) = await client.CreateAndLoginUserAsync(_factory.Services);
        client.UseBearer(access);

        // 修改密码
        await client.PostAsJsonAsync("/api/auth/change-password", new
        {
            currentPassword = "User@12345",
            newPassword = "NewPass@123456"
        });

        // 使用旧密码登录应该失败
        var loginResp = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username = userName,
            password = "User@12345"  // 旧密码
        });
        
        Assert.Equal(HttpStatusCode.Unauthorized, loginResp.StatusCode);
    }
}

