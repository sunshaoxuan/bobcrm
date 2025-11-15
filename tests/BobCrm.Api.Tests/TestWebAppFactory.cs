using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using BobCrm.Api.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Base;
using Microsoft.EntityFrameworkCore;
using BobCrm.Api.Services;

// 测试数据库策略：
// 1. 使用固定的测试数据库名称（bobcrm_test），与开发环境（bobcrm）完全隔离
// 2. 每个测试类启动时重建数据库（RecreateAsync），确保测试独立性
// 3. 测试数据会保留在bobcrm_test中，方便调试
// 4. 使用 scripts/cleanup-test-data.ps1 清理测试数据库
// 5. 开发环境使用 bobcrm 数据库，永远不会被测试污染

namespace BobCrm.Api.Tests;

public class TestWebAppFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // 使用Development环境以启用admin/debug端点
        builder.UseEnvironment("Development");
        
        // 强制使用测试数据库配置（必须在base.CreateHost之前设置，确保最高优先级）
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // 清除所有现有配置，确保测试配置优先
            var testConfig = new Dictionary<string, string?>
            {
                ["Db:Provider"] = "postgres",
                ["ConnectionStrings:Default"] = "Host=localhost;Port=5432;Database=bobcrm_test;Username=postgres;Password=postgres",
                ["Db:SkipInit"] = "true",
                ["Jwt:Key"] = "dev-secret-change-in-prod-1234567890", // 与开发环境保持一致，避免JWT签名不匹配
                ["Jwt:Issuer"] = "BobCrm",
                ["Jwt:Audience"] = "BobCrmUsers",
                ["Jwt:AccessMinutes"] = "60",
                ["Jwt:RefreshDays"] = "7"
            };
            
            // 添加内存配置作为最高优先级
            config.AddInMemoryCollection(testConfig!);
        });
        
        var host = base.CreateHost(builder);
        
        // Reset and seed database to a clean state
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            DatabaseInitializer.RecreateAsync(db).GetAwaiter().GetResult();
            // seed admin user/role and grant access to all customers
            var um = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var rm = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            if (!rm.RoleExistsAsync("admin").GetAwaiter().GetResult())
            {
                rm.CreateAsync(new IdentityRole("admin")).GetAwaiter().GetResult();
            }
            var admin = um.FindByNameAsync("admin").GetAwaiter().GetResult();
            if (admin == null)
            {
                admin = new IdentityUser { UserName = "admin", Email = "admin@local", EmailConfirmed = true };
                um.CreateAsync(admin, "Admin@12345").GetAwaiter().GetResult();
                um.AddToRoleAsync(admin, "admin").GetAwaiter().GetResult();
            }
            else if (!admin.EmailConfirmed)
            {
                admin.EmailConfirmed = true; um.UpdateAsync(admin).GetAwaiter().GetResult();
            }

            var repo = scope.ServiceProvider.GetRequiredService<IRepository<CustomerAccess>>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            // 禁用查询过滤器，测试初始化时没有 HttpContext
            var customers = db.Customers.IgnoreQueryFilters().Select(c => c.Id).ToList();
            foreach (var cid in customers)
            {
                var exists = repo.Query(a => a.CustomerId == cid && a.UserId == admin!.Id).Any();
                if (!exists)
                {
                    repo.AddAsync(new CustomerAccess { CustomerId = cid, UserId = admin!.Id, CanEdit = true }).GetAwaiter().GetResult();
                }
            }
            uow.SaveChangesAsync().GetAwaiter().GetResult();

            var accessService = scope.ServiceProvider.GetRequiredService<AccessService>();
            accessService.SeedSystemAdministratorAsync().GetAwaiter().GetResult();
        }
        return host;
    }
}
