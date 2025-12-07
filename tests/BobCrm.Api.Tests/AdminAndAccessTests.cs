using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Tests;

public class AdminAndAccessTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    public AdminAndAccessTests(TestWebAppFactory factory) => _factory = factory;

    [Fact(Skip = "此测试会重建数据库并破坏其他测试的状态，仅在隔离运行时使用")]
    public async Task Admin_Db_Health_And_Recreate()
    {
        var client = _factory.CreateClient();
        // ensure admin exists and is active
        using (var scope = _factory.Services.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var um = sp.GetRequiredService<UserManager<IdentityUser>>();
            var rm = sp.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await rm.RoleExistsAsync("admin")) await rm.CreateAsync(new IdentityRole("admin"));
            var admin = await um.FindByNameAsync("admin");
            if (admin == null)
            {
                admin = new IdentityUser{ UserName = "admin", Email = "admin@local", EmailConfirmed = true };
                await um.CreateAsync(admin, "Admin@12345");
                await um.AddToRoleAsync(admin, "admin");
            }
            else if (!admin.EmailConfirmed)
            {
                admin.EmailConfirmed = true; await um.UpdateAsync(admin);
            }
            // ensure password is set to known value
            if (!await um.CheckPasswordAsync(admin, "Admin@12345"))
            {
                var hasPwd = await um.HasPasswordAsync(admin);
                if (hasPwd)
                {
                    await um.RemovePasswordAsync(admin);
                }
                await um.AddPasswordAsync(admin, "Admin@12345");
            }
            // ensure role
            if (!await um.IsInRoleAsync(admin, "admin"))
            {
                await um.AddToRoleAsync(admin, "admin");
            }
        }
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var health = await client.GetFromJsonAsync<JsonElement>("/api/admin/db/health");
        Assert.True(health.TryGetProperty("counts", out var counts));
        Assert.True(counts.GetProperty("customers").GetInt32() >= 2);

        // recreate allowed in Development
        var rec = await client.PostAsync("/api/admin/db/recreate", null);
        rec.EnsureSuccessStatusCode();

        // 重建数据库后，重新初始化admin用户和授予访问权限
        using (var scope = _factory.Services.CreateScope())
        {
            var sp = scope.ServiceProvider;
            var um = sp.GetRequiredService<UserManager<IdentityUser>>();
            var rm = sp.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await rm.RoleExistsAsync("admin")) await rm.CreateAsync(new IdentityRole("admin"));
            var admin = await um.FindByNameAsync("admin");
            if (admin == null)
            {
                admin = new IdentityUser{ UserName = "admin", Email = "admin@local", EmailConfirmed = true };
                await um.CreateAsync(admin, "Admin@12345");
                await um.AddToRoleAsync(admin, "admin");
            }
            // 授予admin访问所有客户的权限
            var db = sp.GetRequiredService<BobCrm.Api.Infrastructure.AppDbContext>();
            var repo = sp.GetRequiredService<BobCrm.Api.Core.Persistence.IRepository<BobCrm.Api.Base.CustomerAccess>>();
            var custIds = db.Customers.IgnoreQueryFilters().Select(c => c.Id).ToList();
            foreach (var cid in custIds)
            {
                var exists = repo.Query(a => a.CustomerId == cid && a.UserId == admin.Id).Any();
                if (!exists)
                {
                    await repo.AddAsync(new BobCrm.Api.Base.CustomerAccess { CustomerId = cid, UserId = admin.Id, CanEdit = true });
                }
            }
            await sp.GetRequiredService<BobCrm.Api.Core.Persistence.IUnitOfWork>().SaveChangesAsync();
        }
    }

    [Fact]
    public async Task CustomerAccess_AdminOnly()
    {
        var client = _factory.CreateClient();

        // Now simulate non-admin by logging out and creating a client without bearer or with bearer of admin but name!=admin is not easy; we can use access check on endpoint which validates name==admin or role==admin.
        // We will request without admin token and expect 401 -> authorization required.
        var client2 = _factory.CreateClient();
        var res403get = await client2.GetAsync($"/api/customers/1/access");
        Assert.Equal(HttpStatusCode.Unauthorized, res403get.StatusCode);

        // Authenticated non-admin -> 403
        var userClient = _factory.CreateClient();
        var username = $"user_{Guid.NewGuid():N}";
        var email = $"{username}@local";
        var password = "User@12345";
        var reg = await userClient.PostAsJsonAsync("/api/auth/register", new { username, password, email });
        reg.EnsureSuccessStatusCode();
        using (var scope = _factory.Services.CreateScope())
        {
            var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var u = await um.FindByNameAsync(username);
            var code = await um.GenerateEmailConfirmationTokenAsync(u!);
            var act = await userClient.GetAsync($"/api/auth/activate?userId={Uri.EscapeDataString(u!.Id)}&code={Uri.EscapeDataString(code)}");
            act.EnsureSuccessStatusCode();
        }
        var login = await userClient.PostAsJsonAsync("/api/auth/login", new { username, password });
        login.EnsureSuccessStatusCode();
        var tokenJson = (await login.ReadAsJsonAsync()).UnwrapData();
        var token = tokenJson.GetProperty("accessToken").GetString();
        userClient.UseBearer(token!);
        // 使用 admin 客户端获取客户列表（普通用户可能看不到客户）
        var adminClient = _factory.CreateClient();
        var (adminAccess, _) = await adminClient.LoginAsAdminAsync();
        adminClient.UseBearer(adminAccess);
        var customers = await adminClient.GetFromJsonAsync<JsonElement>("/api/customers");
        Assert.True(customers.GetArrayLength() > 0, "客户列表应该包含至少一个客户");
        var customerId = customers[0].GetProperty("id").GetInt32();
        var res403getAuth = await userClient.GetAsync($"/api/customers/{customerId}/access");
        Assert.Equal(HttpStatusCode.Forbidden, res403getAuth.StatusCode);
        var res403postAuth = await userClient.PostAsJsonAsync($"/api/customers/{customerId}/access", new { userId = "some-user-id", canEdit = true });
        Assert.Equal(HttpStatusCode.Forbidden, res403postAuth.StatusCode);
    }
}
