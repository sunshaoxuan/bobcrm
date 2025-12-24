using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace BobCrm.Api.Tests;

public class AccessIntegrationTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    public AccessIntegrationTests(TestWebAppFactory factory) => _factory = factory;

    private async Task<(string userId, string username, string access)> CreateAndLoginUserAsync()
    {
        var client = _factory.CreateClient();
        return await client.CreateAndLoginUserAsync(_factory.Services);
    }

    [Fact]
    public async Task PutCustomer_Respects_CustomerAccess_CanEdit()
    {
        var admin = _factory.CreateClient();
        var (adminAccess, _) = await admin.LoginAsAdminAsync();
        admin.UseBearer(adminAccess);

        var (userId, _, userAccess) = await CreateAndLoginUserAsync();
        var userClient = _factory.CreateClient();
        userClient.UseBearer(userAccess);

        // 使用 admin 客户端获取客户列表（普通用户没有权限会看到空列表）
        var listResp = await admin.GetAsync("/api/customers");
        listResp.EnsureSuccessStatusCode();
        var list = await listResp.ReadDataAsJsonAsync();
        Assert.True(list.GetArrayLength() > 0, "客户列表应该包含至少一个客户");
        var id = list[0].GetProperty("id").GetInt32();

        // ensure at least one access row exists, but not for this user -> user cannot edit (403)
        var setAdmin = await admin.PostAsJsonAsync($"/api/customers/{id}/access", new { userId = "some-other-user", canEdit = true });
        setAdmin.EnsureSuccessStatusCode();

        // direct update without expectedVersion should still enforce access and return 403
        var denied = await userClient.PutAsJsonAsync($"/api/customers/{id}", new { fields = new[] { new { key = "email", value = "u@local.com" } } });
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        // grant edit to this user -> now OK
        var grant = await admin.PostAsJsonAsync($"/api/customers/{id}/access", new { userId, canEdit = true });
        grant.EnsureSuccessStatusCode();
        var ok = await userClient.PutAsJsonAsync($"/api/customers/{id}", new { fields = new[] { new { key = "email", value = "u@local.com" } } });
        ok.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Layout_Save_Default_Forbidden_For_NonAdmin()
    {
        var admin = _factory.CreateClient();
        var (adminAccess, _) = await admin.LoginAsAdminAsync();
        admin.UseBearer(adminAccess);

        var (_, _, userAccess) = await CreateAndLoginUserAsync();
        var userClient = _factory.CreateClient();
        userClient.UseBearer(userAccess);
        
        // 使用 admin 客户端获取客户列表
        var listResp = await admin.GetAsync("/api/customers");
        listResp.EnsureSuccessStatusCode();
        var list = await listResp.ReadDataAsJsonAsync();
        Assert.True(list.GetArrayLength() > 0, "客户列表应该包含至少一个客户");
        var id = list[0].GetProperty("id").GetInt32();
        var res = await userClient.PostAsJsonAsync($"/api/layout/{id}?scope=default", new { mode = "flow", items = new { email = new { order = 0, w = 6 } } });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
