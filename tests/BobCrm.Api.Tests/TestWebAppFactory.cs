using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using BobCrm.Api.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Domain;
using Microsoft.EntityFrameworkCore;

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
        // 使用Testing环境，自动加载appsettings.Testing.json
        builder.UseEnvironment("Testing");
        
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
        }
        return host;
    }
}
