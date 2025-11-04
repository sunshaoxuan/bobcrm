using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BobCrm.Api.Tests;

public class LayoutTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    public LayoutTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Layout_CRUD_User_And_Default()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        // choose a customer
        var list = await client.GetFromJsonAsync<JsonElement>("/api/customers");
        Assert.True(list.GetArrayLength() > 0, "客户列表应该包含至少一个客户");
        var id = list[0].GetProperty("id").GetInt32();

        // initial effective layout (likely empty)
        var eff = await client.GetFromJsonAsync<JsonElement>($"/api/layout/{id}");
        Assert.True(eff.ValueKind == JsonValueKind.Object);

        // save user layout
        var saveUser = await client.PostAsJsonAsync($"/api/layout/{id}", new { mode = "flow", items = new { email = new { order = 0, w = 6 } } });
        saveUser.EnsureSuccessStatusCode();

        // get user scope
        var userLayout = await client.GetFromJsonAsync<JsonElement>($"/api/layout/{id}?scope=user");
        Assert.Equal("flow", userLayout.GetProperty("mode").GetString());

        // save default layout (admin only)
        var saveDefault = await client.PostAsJsonAsync($"/api/layout/{id}?scope=default", new { mode = "free", items = new { email = new { x = 0, y = 0, w = 3, h = 1 } } });
        saveDefault.EnsureSuccessStatusCode();

        // delete user layout -> effective should fallback to default (non-empty)
        var delUser = await client.DeleteAsync($"/api/layout/{id}");
        delUser.EnsureSuccessStatusCode();
        var eff2 = await client.GetFromJsonAsync<JsonElement>($"/api/layout/{id}");
        Assert.True(eff2.TryGetProperty("items", out _));

        // delete default layout as admin
        var delDefault = await client.DeleteAsync($"/api/layout/{id}?scope=default");
        delDefault.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Layout_Generate_Save_And_Permissions()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var list = await client.GetFromJsonAsync<JsonElement>("/api/customers");
        Assert.True(list.GetArrayLength() > 0, "客户列表应该包含至少一个客户");
        var id = list[0].GetProperty("id").GetInt32();

        // generate flow mode and save user
        var genFlow = await client.PostAsJsonAsync($"/api/layout/{id}/generate", new { tags = new[] { "常用" }, mode = "flow", save = true, scope = "user" });
        genFlow.EnsureSuccessStatusCode();

        // generate free mode and save default (admin)
        var genDefault = await client.PostAsJsonAsync($"/api/layout/{id}/generate", new { tags = new[] { "常用" }, mode = "free", save = true, scope = "default" });
        genDefault.EnsureSuccessStatusCode();

        // missing tags -> 400
        var bad = await client.PostAsJsonAsync($"/api/layout/{id}/generate", new { tags = Array.Empty<string>() });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }
}
