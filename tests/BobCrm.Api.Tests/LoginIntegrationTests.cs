using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BobCrm.Api.Tests;

/// <summary>
/// 用户登录集成测试
/// 按照 TEST-05-登录认证集成测试.md 规划实现
/// </summary>
public class LoginIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    
    public LoginIntegrationTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// TC-01: 后端 API 直接登录 - 正确凭据
    /// 目的: 验证后端 API 能够正确处理有效的登录请求
    /// </summary>
    [Fact]
    public async Task TC01_BackendApiLogin_WithValidCredentials_Returns200()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginRequest = new { username = "admin", password = "Admin@12345" };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var json = await response.ReadAsJsonAsync();
        var data = json.UnwrapData();
        
        // 验证响应体包含 accessToken 字段（JWT 令牌）
        Assert.True(data.TryGetProperty("accessToken", out var accessTokenProp), 
            "响应应包含 accessToken 字段");
        var accessToken = accessTokenProp.GetString();
        Assert.False(string.IsNullOrWhiteSpace(accessToken), 
            "accessToken 不应为空");
        
        // 验证响应体包含 refreshToken 字段
        Assert.True(data.TryGetProperty("refreshToken", out var refreshTokenProp), 
            "响应应包含 refreshToken 字段");
        var refreshToken = refreshTokenProp.GetString();
        Assert.False(string.IsNullOrWhiteSpace(refreshToken), 
            "refreshToken 不应为空");
        
        // 验证响应体包含 user 对象，其中 userName 为 "admin"
        Assert.True(data.TryGetProperty("user", out var userProp), 
            "响应应包含 user 对象");
        Assert.True(userProp.TryGetProperty("userName", out var userNameProp) || 
                    userProp.TryGetProperty("username", out userNameProp), 
            "user 对象应包含 userName 或 username 字段");
        var userName = userNameProp.GetString();
        Assert.Equal("admin", userName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// TC-02: 后端 API 直接登录 - 错误密码
    /// 目的: 验证后端 API 能够正确拒绝无效凭据
    /// </summary>
    [Fact]
    public async Task TC02_BackendApiLogin_WithWrongPassword_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginRequest = new { username = "admin", password = "wrongpassword" };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        
        var json = await response.ReadAsJsonAsync();
        // 验证错误响应包含 code 字段
        Assert.True(json.TryGetProperty("code", out _), 
            "错误响应应包含 code 字段");
    }

    /// <summary>
    /// TC-02 变体: 后端 API 直接登录 - 错误用户名
    /// 目的: 验证后端 API 能够正确拒绝不存在的用户名
    /// </summary>
    [Fact]
    public async Task TC02_Variant_BackendApiLogin_WithNonexistentUsername_Returns401()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginRequest = new { username = "nonexistent_user_12345", password = "AnyPassword@123" };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        
        var json = await response.ReadAsJsonAsync();
        // 验证错误响应包含 code 字段
        Assert.True(json.TryGetProperty("code", out _), 
            "错误响应应包含 code 字段");
    }

    /// <summary>
    /// TC-02 变体: 后端 API 直接登录 - 空用户名
    /// 目的: 验证后端 API 能够正确处理空用户名请求
    /// </summary>
    [Fact]
    public async Task TC02_Variant_BackendApiLogin_WithEmptyUsername_Returns400()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginRequest = new { username = "", password = "AnyPassword@123" };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// TC-02 变体: 后端 API 直接登录 - 邮箱未确认
    /// 目的: 验证后端 API 能够正确拒绝未激活账户的登录请求
    /// </summary>
    [Fact]
    public async Task TC02_Variant_BackendApiLogin_WithUnconfirmedEmail_Returns400()
    {
        // Arrange
        var client = _factory.CreateClient();
        var username = $"user_{Guid.NewGuid():N}";
        var email = $"{username}@local";
        var password = "User@12345";

        // 注册用户但不激活
        var regResponse = await client.PostAsJsonAsync("/api/auth/register", 
            new { username, password, email });
        regResponse.EnsureSuccessStatusCode();

        // Act - 尝试登录未激活账户
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", 
            new { username, password });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, loginResponse.StatusCode);
        
        var json = await loginResponse.ReadAsJsonAsync();
        Assert.True(json.TryGetProperty("code", out var codeProp), 
            "错误响应应包含 code 字段");
        var code = codeProp.GetString();
        Assert.Equal("EMAIL_NOT_CONFIRMED", code, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// TC-03: 前端代理登录 - 正确凭据
    /// 注意: 此测试需要实际运行的前端应用（端口3000），在单元测试框架中无法直接测试
    /// 实际的前端代理测试需要通过以下方式执行：
    /// 1. 启动完整环境: ./scripts/dev.ps1 -Action start -Detached
    /// 2. 使用 PowerShell 脚本测试: ./scripts/verify-auth.ps1
    /// 3. 或使用浏览器自动化工具（如 Playwright、Selenium）进行 E2E 测试
    /// </summary>
    [Fact(Skip = "需要实际运行的前端应用，无法在单元测试框架中直接测试")]
    public Task TC03_FrontendProxyLogin_WithValidCredentials_Returns200()
    {
        // 此测试需要实际运行的前端 Blazor 应用（端口3000）
        // 前端应用会将 /api/* 请求代理到后端 API（端口5200）
        // 测试步骤：
        // 1. 确保前端应用运行在 http://localhost:3000
        // 2. 发送 POST 请求到 http://localhost:3000/api/auth/login
        // 3. 验证响应状态码为 200 OK
        // 4. 验证响应体包含 token 和 user 字段
        
        // 示例代码（需要实际运行的服务）：
        // var client = new HttpClient { BaseAddress = new Uri("http://localhost:3000") };
        // var response = await client.PostAsJsonAsync("/api/auth/login", 
        //     new { username = "admin", password = "Admin@12345" });
        // Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Task.CompletedTask;
    }

    /// <summary>
    /// TC-04: 前端代理登录 - 错误用户名
    /// 注意: 此测试需要实际运行的前端应用（端口3000），在单元测试框架中无法直接测试
    /// </summary>
    [Fact(Skip = "需要实际运行的前端应用，无法在单元测试框架中直接测试")]
    public Task TC04_FrontendProxyLogin_WithNonexistentUsername_Returns401()
    {
        // 此测试需要实际运行的前端 Blazor 应用（端口3000）
        // 测试步骤：
        // 1. 确保前端应用运行在 http://localhost:3000
        // 2. 发送 POST 请求到 http://localhost:3000/api/auth/login，使用不存在的用户名
        // 3. 验证响应状态码为 401 Unauthorized
        return Task.CompletedTask;
    }

    /// <summary>
    /// TC-05: 浏览器 UI 登录流程
    /// 注意: 此测试需要浏览器自动化工具，不在单元测试范围内
    /// 建议使用 Playwright、Selenium 或 Cypress 等工具进行 E2E 测试
    /// </summary>
    [Fact(Skip = "需要浏览器自动化工具，不在单元测试范围内")]
    public Task TC05_BrowserUILoginFlow_Succeeds()
    {
        // 此测试需要浏览器自动化工具
        // 测试步骤：
        // 1. 打开浏览器，导航到 http://localhost:3000/login
        // 2. 在用户名输入框中输入 admin
        // 3. 在密码输入框中输入 Admin@12345
        // 4. 点击登录按钮
        // 5. 等待页面跳转
        // 6. 验证页面成功跳转到仪表板（/dashboard 或 /）
        // 7. 验证无错误提示显示
        // 8. 验证用户名显示在页面右上角
        return Task.CompletedTask;
    }

    /// <summary>
    /// 额外测试: 验证登录响应格式符合 API 规范
    /// </summary>
    [Fact]
    public async Task LoginResponse_ShouldHaveCorrectStructure()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginRequest = new { username = "admin", password = "Admin@12345" };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();

        // Assert - 验证响应结构
        var json = await response.ReadAsJsonAsync();
        
        // 验证响应包含 data 包装器
        Assert.True(json.TryGetProperty("data", out var dataProp), 
            "响应应包含 data 包装器");
        
        var data = dataProp;
        
        // 验证 data 中包含 accessToken
        Assert.True(data.TryGetProperty("accessToken", out _), 
            "data 应包含 accessToken");
        
        // 验证 data 中包含 refreshToken
        Assert.True(data.TryGetProperty("refreshToken", out _), 
            "data 应包含 refreshToken");
        
        // 验证 data 中包含 user 对象
        Assert.True(data.TryGetProperty("user", out var userProp), 
            "data 应包含 user 对象");
        
        // 验证 user 对象包含必要字段
        var user = userProp;
        Assert.True(user.TryGetProperty("userName", out _) || 
                    user.TryGetProperty("username", out _), 
            "user 应包含 userName 或 username");
        Assert.True(user.TryGetProperty("id", out _) || 
                    user.TryGetProperty("Id", out _), 
            "user 应包含 id 或 Id");
    }

    /// <summary>
    /// 额外测试: 验证默认管理员账户种子数据正确生成
    /// </summary>
    [Fact]
    public async Task DefaultAdminAccount_ShouldBeSeeded()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - 使用默认管理员凭据登录
        var response = await client.PostAsJsonAsync("/api/auth/login", 
            new { username = "admin", password = "Admin@12345" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var json = await response.ReadAsJsonAsync();
        var data = json.UnwrapData();
        var user = data.GetProperty("user");
        
        // 验证管理员账户存在且可以登录
        var userName = user.TryGetProperty("userName", out var unProp) 
            ? unProp.GetString() 
            : user.GetProperty("username").GetString();
        Assert.Equal("admin", userName, StringComparer.OrdinalIgnoreCase);
        
        // 验证管理员账户已激活
        if (user.TryGetProperty("emailConfirmed", out var ecProp))
        {
            Assert.True(ecProp.GetBoolean(), "管理员账户应已激活邮箱");
        }
    }
}

