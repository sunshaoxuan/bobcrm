using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Linq;
using BobCrm.Api.Infrastructure;
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
    private readonly string _sqliteDbPath = Path.Combine(Path.GetTempPath(), $"bobcrm_test_{Guid.NewGuid():N}.db");
    private string SqliteConnectionString => $"Data Source={_sqliteDbPath}";
    public string ServerConnectionString => SqliteConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseTestServer();

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.Sources.Clear();

            var testConfig = new Dictionary<string, string?>
            {
                ["Db:Provider"] = "sqlite",
                ["ConnectionStrings:Default"] = SqliteConnectionString,
                ["Db:SkipInit"] = "true",
                ["Jwt:Key"] = "dev-secret-change-in-prod-1234567890",
                ["Jwt:Issuer"] = "BobCrm",
                ["Jwt:Audience"] = "BobCrmUsers",
                ["Jwt:AccessMinutes"] = "60",
                ["Jwt:RefreshDays"] = "7"
            };

            config.AddInMemoryCollection(testConfig!);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(SqliteConnectionString));
            services.AddScoped<DbContext>(sp => sp.GetRequiredService<AppDbContext>());

            // Ensure fresh database for each test run and seed baseline data
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureDeleted();
            db.Database.Migrate();
            DatabaseInitializer.InitializeAsync(db).GetAwaiter().GetResult();

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
                admin.EmailConfirmed = true;
                um.UpdateAsync(admin).GetAwaiter().GetResult();
            }

            var repo = scope.ServiceProvider.GetRequiredService<IRepository<CustomerAccess>>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
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
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            if (File.Exists(_sqliteDbPath))
            {
                File.Delete(_sqliteDbPath);
            }
        }
        catch
        {
        }
    }

    protected override TestServer CreateServer(IWebHostBuilder builder)
    {
        return new TestServer(builder);
    }
}
