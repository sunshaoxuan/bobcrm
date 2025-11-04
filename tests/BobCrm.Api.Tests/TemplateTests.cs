using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BobCrm.Api.Tests;

/// <summary>
/// 全局模板（不依赖具体客户）测试
/// </summary>
public class TemplateTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    public TemplateTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Template_Default_Exists_After_Initialization()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        // 获取默认模板（customerId = 0, scope = default）
        var resp = await client.GetAsync("/api/layout/customer?scope=default");
        resp.EnsureSuccessStatusCode();

        var layout = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Object, layout.ValueKind);
        
        // 验证有 mode 和 items
        Assert.True(layout.TryGetProperty("mode", out var mode));
        Assert.Equal("flow", mode.GetString());
        Assert.True(layout.TryGetProperty("items", out var items));
        Assert.True(items.EnumerateObject().Count() > 0, "默认模板应该包含至少一个字段");
    }

    [Fact]
    public async Task Template_User_Can_Save_Personal_Template()
    {
        var client = _factory.CreateClient();
        var (userId, _, access) = await client.CreateAndLoginUserAsync(_factory.Services);
        client.UseBearer(access);

        // 保存用户个人模板
        var personalTemplate = new
        {
            mode = "flow",
            items = new
            {
                email = new { order = 0, w = 12 },
                link = new { order = 1, w = 6 }
            }
        };

        var saveResp = await client.PostAsJsonAsync("/api/layout/customer", personalTemplate);
        saveResp.EnsureSuccessStatusCode();

        // 获取用户模板
        var getResp = await client.GetAsync("/api/layout/customer?scope=user");
        getResp.EnsureSuccessStatusCode();

        var layout = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(layout.TryGetProperty("items", out var items));
        Assert.True(items.TryGetProperty("email", out _));
        Assert.Equal(12, items.GetProperty("email").GetProperty("w").GetInt32());
    }

    [Fact]
    public async Task Template_Admin_Can_Save_As_Default()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        // 管理员保存为默认模板
        var defaultTemplate = new
        {
            mode = "flow",
            items = new
            {
                email = new { order = 0, w = 6 },
                description = new { order = 1, w = 12 }
            }
        };

        var saveResp = await client.PostAsJsonAsync("/api/layout/customer?scope=default", defaultTemplate);
        saveResp.EnsureSuccessStatusCode();

        // 验证默认模板已更新
        var getResp = await client.GetAsync("/api/layout/customer?scope=default");
        getResp.EnsureSuccessStatusCode();

        var layout = await getResp.Content.ReadFromJsonAsync<JsonElement>();
        var items = layout.GetProperty("items");
        Assert.True(items.TryGetProperty("email", out _));
        Assert.True(items.TryGetProperty("description", out _));
    }

    [Fact]
    public async Task Template_NonAdmin_Cannot_Save_As_Default()
    {
        var client = _factory.CreateClient();
        var (userId, _, access) = await client.CreateAndLoginUserAsync(_factory.Services);
        client.UseBearer(access);

        // 普通用户尝试保存为默认模板
        var template = new
        {
            mode = "flow",
            items = new { email = new { order = 0, w = 6 } }
        };

        var saveResp = await client.PostAsJsonAsync("/api/layout/customer?scope=default", template);
        
        // 应该返回 403 Forbidden
        Assert.Equal(HttpStatusCode.Forbidden, saveResp.StatusCode);
    }

    [Fact]
    public async Task Template_User_Reset_Returns_To_Default()
    {
        var client = _factory.CreateClient();
        var (userId, _, access) = await client.CreateAndLoginUserAsync(_factory.Services);
        client.UseBearer(access);

        // 用户保存个人模板
        var personalTemplate = new
        {
            mode = "flow",
            items = new { email = new { order = 0, w = 12 } }
        };
        await client.PostAsJsonAsync("/api/layout/customer", personalTemplate);

        // 验证用户模板存在
        var userLayout = await client.GetFromJsonAsync<JsonElement>("/api/layout/customer?scope=user");
        Assert.True(userLayout.TryGetProperty("items", out _));

        // 删除用户模板（重置）
        var deleteResp = await client.DeleteAsync("/api/layout/customer");
        deleteResp.EnsureSuccessStatusCode();

        // 获取 effective 模板，应该回退到默认
        var effectiveLayout = await client.GetFromJsonAsync<JsonElement>("/api/layout/customer?scope=effective");
        Assert.Equal(JsonValueKind.Object, effectiveLayout.ValueKind);
        
        // 应该包含默认模板的字段
        if (effectiveLayout.TryGetProperty("items", out var items))
        {
            // 默认模板应该包含预定义的字段
            Assert.True(items.EnumerateObject().Count() > 0, "默认模板应该包含字段");
        }
    }

    [Fact]
    public async Task Template_Effective_Prioritizes_User_Over_Default()
    {
        var client = _factory.CreateClient();
        var (userId, _, access) = await client.CreateAndLoginUserAsync(_factory.Services);
        client.UseBearer(access);

        // 获取初始 effective（应该是默认模板）
        var defaultEffective = await client.GetFromJsonAsync<JsonElement>("/api/layout/customer?scope=effective");
        
        // 保存用户个人模板
        var personalTemplate = new
        {
            mode = "flow",
            items = new { email = new { order = 0, w = 12 } }
        };
        await client.PostAsJsonAsync("/api/layout/customer", personalTemplate);

        // 获取 effective，应该返回用户模板
        var userEffective = await client.GetFromJsonAsync<JsonElement>("/api/layout/customer?scope=effective");
        var items = userEffective.GetProperty("items");
        Assert.True(items.TryGetProperty("email", out var email));
        Assert.Equal(12, email.GetProperty("w").GetInt32());
    }

    [Fact]
    public async Task Template_Independent_Of_Customer_Data()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        // 即使没有客户数据，也应该能获取模板
        var resp = await client.GetAsync("/api/layout/customer?scope=default");
        resp.EnsureSuccessStatusCode();

        var layout = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(JsonValueKind.Null, layout.ValueKind);
        Assert.NotEqual(JsonValueKind.Undefined, layout.ValueKind);
    }
}

