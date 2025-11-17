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
            // ⭐ 关键修复：清除所有现有配置源，然后只添加测试配置
            // 这样可以防止 appsettings.Development.json 中的 bobcrm 数据库配置被加载
            config.Sources.Clear();

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

            // 添加内存配置作为唯一配置源
            config.AddInMemoryCollection(testConfig!);
        });

        // ⭐ 关键修复：在启动应用程序之前初始化数据库
        // 这样可以避免 I18nEndpoints.ResolveDocumentationLanguage() 在启动时查询不存在的表
        var connectionString = "Host=localhost;Port=5432;Database=bobcrm_test;Username=postgres;Password=postgres";

        // 步骤1：强制终止所有连接并删除/创建数据库（使用原始SQL，不依赖EF Core）
        var connBuilder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString) { Database = "postgres" };
        using (var adminConn = new Npgsql.NpgsqlConnection(connBuilder.ToString()))
        {
            adminConn.Open();

            // 处理 bobcrm_test 数据库
            using (var cmd = new Npgsql.NpgsqlCommand(
                "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = 'bobcrm_test' AND pid <> pg_backend_pid();",
                adminConn))
            {
                try { cmd.ExecuteNonQuery(); } catch { }
            }
            using (var cmd = new Npgsql.NpgsqlCommand("DROP DATABASE IF EXISTS bobcrm_test;", adminConn))
            {
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new Npgsql.NpgsqlCommand("CREATE DATABASE bobcrm_test;", adminConn))
            {
                cmd.ExecuteNonQuery();
            }

            // ⭐ 同时确保 bobcrm 数据库存在（防止配置加载时连接失败）
            // 某些测试环境下可能会因为 appsettings.Development.json 而尝试连接 bobcrm
            using (var cmd = new Npgsql.NpgsqlCommand(
                "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = 'bobcrm' AND pid <> pg_backend_pid();",
                adminConn))
            {
                try { cmd.ExecuteNonQuery(); } catch { }
            }
            using (var cmd = new Npgsql.NpgsqlCommand("DROP DATABASE IF EXISTS bobcrm;", adminConn))
            {
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new Npgsql.NpgsqlCommand("CREATE DATABASE bobcrm;", adminConn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        // 步骤2：应用迁移并初始化数据（两个数据库都需要初始化）
        var testDbOptions = new DbContextOptionsBuilder<AppDbContext>();
        testDbOptions.UseNpgsql(connectionString);
        using (var tempDb = new AppDbContext(testDbOptions.Options))
        {
            // 初始化 bobcrm_test 数据库
            DatabaseInitializer.InitializeAsync(tempDb).GetAwaiter().GetResult();
        }

        var devDbOptions = new DbContextOptionsBuilder<AppDbContext>();
        devDbOptions.UseNpgsql("Host=localhost;Port=5432;Database=bobcrm;Username=postgres;Password=postgres");
        using (var devDb = new AppDbContext(devDbOptions.Options))
        {
            // 初始化 bobcrm 数据库（防止某些测试意外连接到它）
            DatabaseInitializer.InitializeAsync(devDb).GetAwaiter().GetResult();
        }

        var host = base.CreateHost(builder);

        // Seed admin user/role and grant access to all customers
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
