using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BobCrm.Api.Tests;

/// <summary>
/// 布局尺寸和单位测试（Width/WidthUnit/Height/HeightUnit）
/// </summary>
public class LayoutDimensionTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    public LayoutDimensionTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Layout_Preserves_Width_And_WidthUnit_Percent()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        // 获取一个客户 ID
        var customersResp = await client.GetAsync("/api/customers");
        customersResp.EnsureSuccessStatusCode();
        var customers = await customersResp.ReadDataAsJsonAsync();
        Assert.True(customers.GetArrayLength() > 0);
        var customerId = customers[0].GetProperty("id").GetInt32();

        // 保存布局：宽度使用百分比
        var layoutWithPercent = new
        {
            mode = "free",
            items = new
            {
                email = new 
                { 
                    x = 0, 
                    y = 0, 
                    w = 6,          // 宽度值
                    wUnit = "%",    // 百分比单位
                    h = 1,
                    hUnit = "px"    // 像素单位
                }
            }
        };

        var saveResponse = await client.PostAsJsonAsync($"/api/layout/{customerId}", layoutWithPercent);
        saveResponse.EnsureSuccessStatusCode();

        // 重新读取布局
        var loadedLayoutResp = await client.GetAsync($"/api/layout/{customerId}?scope=user");
        loadedLayoutResp.EnsureSuccessStatusCode();
        var loadedLayout = await loadedLayoutResp.ReadDataAsJsonAsync();
        
        // 验证布局包含 items.email
        Assert.True(loadedLayout.TryGetProperty("items", out var items));
        Assert.True(items.TryGetProperty("email", out var emailWidget));

        // 验证尺寸和单位被正确保存
        Assert.Equal(6, emailWidget.GetProperty("w").GetInt32());
        Assert.Equal("%", emailWidget.GetProperty("wUnit").GetString());
        Assert.Equal(1, emailWidget.GetProperty("h").GetInt32());
        Assert.Equal("px", emailWidget.GetProperty("hUnit").GetString());
    }

    [Fact]
    public async Task Layout_Preserves_Width_And_WidthUnit_Pixels()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var customersResp = await client.GetAsync("/api/customers");
        customersResp.EnsureSuccessStatusCode();
        var customers = await customersResp.ReadDataAsJsonAsync();
        var customerId = customers[0].GetProperty("id").GetInt32();

        // 保存布局：宽度和高度都使用像素
        var layoutWithPixels = new
        {
            mode = "free",
            items = new
            {
                link = new 
                { 
                    x = 100, 
                    y = 50, 
                    w = 200,        // 宽度值（像素）
                    wUnit = "px",   // 像素单位
                    h = 150,
                    hUnit = "px"
                }
            }
        };

        var saveResponse = await client.PostAsJsonAsync($"/api/layout/{customerId}", layoutWithPixels);
        saveResponse.EnsureSuccessStatusCode();

        // 重新读取
        var loadedLayoutResp = await client.GetAsync($"/api/layout/{customerId}?scope=user");
        loadedLayoutResp.EnsureSuccessStatusCode();
        var loadedLayout = await loadedLayoutResp.ReadDataAsJsonAsync();
        var linkWidget = loadedLayout.GetProperty("items").GetProperty("link");

        Assert.Equal(200, linkWidget.GetProperty("w").GetInt32());
        Assert.Equal("px", linkWidget.GetProperty("wUnit").GetString());
        Assert.Equal(150, linkWidget.GetProperty("h").GetInt32());
        Assert.Equal("px", linkWidget.GetProperty("hUnit").GetString());
    }

    [Fact]
    public async Task Layout_Mixed_Units_In_Different_Widgets()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var customersResp = await client.GetAsync("/api/customers");
        customersResp.EnsureSuccessStatusCode();
        var customers = await customersResp.ReadDataAsJsonAsync();
        var customerId = customers[0].GetProperty("id").GetInt32();

        // 保存布局：不同控件使用不同单位
        var mixedLayout = new
        {
            mode = "free",
            items = new
            {
                email = new { x = 0, y = 0, w = 50, wUnit = "%", h = 1, hUnit = "px" },
                link = new { x = 100, y = 0, w = 300, wUnit = "px", h = 100, hUnit = "px" }
            }
        };

        var saveResp = await client.PostAsJsonAsync($"/api/layout/{customerId}", mixedLayout);
        saveResp.EnsureSuccessStatusCode();

        // 验证读取
        var loadedResp = await client.GetAsync($"/api/layout/{customerId}?scope=user");
        loadedResp.EnsureSuccessStatusCode();
        var loaded = await loadedResp.ReadDataAsJsonAsync();
        var items = loaded.GetProperty("items");

        var email = items.GetProperty("email");
        Assert.Equal("%", email.GetProperty("wUnit").GetString());
        Assert.Equal("px", email.GetProperty("hUnit").GetString());

        var link = items.GetProperty("link");
        Assert.Equal("px", link.GetProperty("wUnit").GetString());
        Assert.Equal("px", link.GetProperty("hUnit").GetString());
    }

    [Fact]
    public async Task Layout_Flow_Mode_Preserves_Width()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var customersResp = await client.GetAsync("/api/customers");
        customersResp.EnsureSuccessStatusCode();
        var customers = await customersResp.ReadDataAsJsonAsync();
        var customerId = customers[0].GetProperty("id").GetInt32();

        // Flow 模式：使用 order 和 w
        var flowLayout = new
        {
            mode = "flow",
            items = new
            {
                email = new { order = 0, w = 6 },
                link = new { order = 1, w = 12 }
            }
        };

        var saveResp = await client.PostAsJsonAsync($"/api/layout/{customerId}", flowLayout);
        saveResp.EnsureSuccessStatusCode();

        var loadedResp = await client.GetAsync($"/api/layout/{customerId}?scope=user");
        loadedResp.EnsureSuccessStatusCode();
        var loaded = await loadedResp.ReadDataAsJsonAsync();
        Assert.Equal("flow", loaded.GetProperty("mode").GetString());

        var items = loaded.GetProperty("items");
        Assert.Equal(6, items.GetProperty("email").GetProperty("w").GetInt32());
        Assert.Equal(12, items.GetProperty("link").GetProperty("w").GetInt32());
    }

    [Fact]
    public async Task Layout_Delete_Then_Get_Returns_Default_Or_Empty()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var customersResp = await client.GetAsync("/api/customers");
        customersResp.EnsureSuccessStatusCode();
        var customers = await customersResp.ReadDataAsJsonAsync();
        var customerId = customers[0].GetProperty("id").GetInt32();

        // 保存用户布局
        var layout = new { mode = "flow", items = new { email = new { order = 0, w = 6 } } };
        var saveResp = await client.PostAsJsonAsync($"/api/layout/{customerId}", layout);
        saveResp.EnsureSuccessStatusCode();

        // 删除用户布局
        var deleteResponse = await client.DeleteAsync($"/api/layout/{customerId}");
        deleteResponse.EnsureSuccessStatusCode();

        // 获取 effective 布局（应该回退到默认或为空）
        var effectiveResp = await client.GetAsync($"/api/layout/{customerId}");
        effectiveResp.EnsureSuccessStatusCode();
        var effective = await effectiveResp.ReadDataAsJsonAsync();
        
        // 如果有默认布局则返回默认，否则返回空对象或基础结构
        Assert.Equal(JsonValueKind.Object, effective.ValueKind);
    }

    [Fact]
    public async Task Layout_User_And_Default_Scope_Are_Independent()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var customersResp = await client.GetAsync("/api/customers");
        customersResp.EnsureSuccessStatusCode();
        var customers = await customersResp.ReadDataAsJsonAsync();
        var customerId = customers[0].GetProperty("id").GetInt32();

        // 保存用户布局
        var userLayout = new { mode = "flow", items = new { email = new { order = 0, w = 6 } } };
        var userSaveResp = await client.PostAsJsonAsync($"/api/layout/{customerId}", userLayout);
        userSaveResp.EnsureSuccessStatusCode();

        // 保存默认布局（admin 权限）
        var defaultLayout = new { mode = "free", items = new { email = new { x = 0, y = 0, w = 100, wUnit = "px", h = 50, hUnit = "px" } } };
        var defaultSaveResp = await client.PostAsJsonAsync($"/api/layout/{customerId}?scope=default", defaultLayout);
        defaultSaveResp.EnsureSuccessStatusCode();

        // 读取用户布局
        var loadedUserResp = await client.GetAsync($"/api/layout/{customerId}?scope=user");
        loadedUserResp.EnsureSuccessStatusCode();
        var loadedUser = await loadedUserResp.ReadDataAsJsonAsync();
        Assert.Equal("flow", loadedUser.GetProperty("mode").GetString());

        // 读取默认布局
        var loadedDefaultResp = await client.GetAsync($"/api/layout/{customerId}?scope=default");
        loadedDefaultResp.EnsureSuccessStatusCode();
        var loadedDefault = await loadedDefaultResp.ReadDataAsJsonAsync();
        Assert.Equal("free", loadedDefault.GetProperty("mode").GetString());
        Assert.Equal("px", loadedDefault.GetProperty("items").GetProperty("email").GetProperty("wUnit").GetString());
    }
}

