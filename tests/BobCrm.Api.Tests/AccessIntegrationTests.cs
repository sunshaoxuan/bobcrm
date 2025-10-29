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
        var username = $"user_{Guid.NewGuid():N}";
        var email = $"{username}@local";
        var password = "User@12345";
        var reg = await client.PostAsJsonAsync("/api/auth/register", new { username, password, email });
        reg.EnsureSuccessStatusCode();
        using (var scope = _factory.Services.CreateScope())
        {
            var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var u = await um.FindByNameAsync(username);
            var code = await um.GenerateEmailConfirmationTokenAsync(u!);
            var act = await client.GetAsync($"/api/auth/activate?userId={Uri.EscapeDataString(u!.Id)}&code={Uri.EscapeDataString(code)}");
            act.EnsureSuccessStatusCode();
            var login = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
            login.EnsureSuccessStatusCode();
            var json = JsonDocument.Parse(await login.Content.ReadAsStringAsync()).RootElement;
            var access = json.GetProperty("accessToken").GetString()!;
            return (u!.Id, username, access);
        }
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

        // pick a customer id (when user has no access rows, list returns all)
        var list = await userClient.GetFromJsonAsync<JsonElement>("/api/customers");
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
        var (_, _, userAccess) = await CreateAndLoginUserAsync();
        var userClient = _factory.CreateClient();
        userClient.UseBearer(userAccess);
        var list = await userClient.GetFromJsonAsync<JsonElement>("/api/customers");
        var id = list[0].GetProperty("id").GetInt32();
        var res = await userClient.PostAsJsonAsync($"/api/layout/{id}?scope=default", new { mode = "flow", items = new { email = new { order = 0, w = 6 } } });
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
