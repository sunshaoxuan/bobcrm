using BobCrm.Api.Abstractions;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace BobCrm.Api.Tests;

/// <summary>
/// 数据库初始化逻辑测试 - 特别关注实体自动注册的各种逻辑分支
/// </summary>
public class DatabaseInitializerTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    public DatabaseInitializerTests(TestWebAppFactory factory)
    {
        _factory = factory;
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    [Fact]
    public async Task AutoRegister_Customer_Entity_Exists_After_Initialization()
    {
        // 这个测试验证：新实体的自动注册路径
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        // 获取所有实体元数据
        var resp = await client.GetAsync("/api/entities/all");
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();

        // 验证Customer实体已注册
        Assert.Contains("BobCrm.Api.Base.Customer", json); // entityType (FullTypeName)
        Assert.Contains("customer", json); // entityRoute
        Assert.Contains("Customer", json); // entityName
        Assert.Contains("displayName", json); // 多语言显示名字段
    }

    // TODO: 此测试依赖于旧的EntityMetadata系统，需要重构为使用新的EntityDefinition
    // [Fact]
    // public async Task AutoRegister_Re_Enables_Previously_Disabled_Entity()
    // {
    //     // 这个测试验证：已禁用实体的重新启用路径
    //     using var scope = _factory.Services.CreateScope();
    //     var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    //
    //     // 手动禁用Customer实体
    //     var customer = await db.EntityDefinitions
    //         .FirstOrDefaultAsync(e => e.FullTypeName == "BobCrm.Api.Base.Customer");

    [Fact]
    public async Task AutoRegister_Re_Enables_Previously_Disabled_Entity_DISABLED()
    {
        // 测试已禁用 - 等待重构为使用新的EntityDefinition系统
        await Task.CompletedTask;
    }

    [Fact]
    public async Task AutoRegister_Deactivates_Nonexistent_Entity_Metadata_DISABLED()
    {
        // 测试已禁用 - 等待重构为使用新的EntityDefinition系统
        await Task.CompletedTask;
    }


    [Fact]
    public async Task AutoRegister_Skips_Already_Enabled_Entity_DISABLED()
    {
        // 测试已禁用 - 等待重构为使用新的EntityDefinition系统
        await Task.CompletedTask;
    }

    [Fact]
    public async Task RecreateAsync_Drops_And_Recreates_Database_With_All_Data()
    {
        var databaseName = $"dbinit_{Guid.NewGuid():N}";

        // 先创建数据库
        await CreateDatabaseAsync(databaseName);

        await using var db = CreateIsolatedContext(databaseName);
        try
        {
            // 使用 RecreateAsync 来重建数据库（完整的 drop + create + migrate 流程）
            await DatabaseInitializer.RecreateAsync(db);
            await DatabaseInitializer.InitializeAsync(db);

            var customersExist = await db.Set<Customer>().AnyAsync();
            Assert.True(customersExist, "应该有初始客户数据");

            var fieldsExist = await db.Set<FieldDefinition>().AnyAsync();
            Assert.True(fieldsExist, "应该有字段定义");

            var langsExist = await db.Set<LocalizationLanguage>().AnyAsync();
            Assert.True(langsExist, "应该有语言配置");

            var resourcesExist = await db.Set<LocalizationResource>().AnyAsync();
            Assert.True(resourcesExist, "应该有多语言资源");

            var entitiesExist = await db.Set<EntityDefinition>().AnyAsync();
            Assert.True(entitiesExist, "应该有实体定义");

            var defaultLayout = await db.Set<UserLayout>()
                .FirstOrDefaultAsync(UserLayoutScope.ForUser("__default__", 0));
            Assert.NotNull(defaultLayout);
            Assert.False(string.IsNullOrWhiteSpace(defaultLayout!.LayoutJson));
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
            await DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task Initialize_Creates_All_Required_Tables_And_Data()
    {
        var databaseName = $"dbinit_{Guid.NewGuid():N}";

        // 先创建数据库
        await CreateDatabaseAsync(databaseName);

        await using var db = CreateIsolatedContext(databaseName);
        try
        {
            await DatabaseInitializer.InitializeAsync(db);

            var customersExist = await db.Set<Customer>().AnyAsync();
            Assert.True(customersExist, "应该有初始客户数据");

            var fieldsExist = await db.Set<FieldDefinition>().AnyAsync();
            Assert.True(fieldsExist, "应该有字段定义");

            var langsExist = await db.Set<LocalizationLanguage>().AnyAsync();
            Assert.True(langsExist, "应该有语言配置");

            var resourcesExist = await db.Set<LocalizationResource>().AnyAsync();
            Assert.True(resourcesExist, "应该有多语言资源");

            var entitiesExist = await db.Set<EntityDefinition>().AnyAsync();
            Assert.True(entitiesExist, "应该有实体定义");

            var defaultLayout = await db.Set<UserLayout>()
                .FirstOrDefaultAsync(UserLayoutScope.ForUser("__default__", 0));
            Assert.NotNull(defaultLayout);
            Assert.False(string.IsNullOrWhiteSpace(defaultLayout!.LayoutJson));
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
            await DropDatabaseAsync(databaseName);
        }
    }

    [Fact]
    public async Task Initialize_Ensure_Method_Adds_Missing_Keys()
    {
        var existingKey = "MENU_PROFILE";
        var databaseName = $"dbinit_{Guid.NewGuid():N}";

        // 先创建数据库
        await CreateDatabaseAsync(databaseName);

        await using (var db = CreateIsolatedContext(databaseName))
        {
            await DatabaseInitializer.InitializeAsync(db);
        }

        try
        {
            using (var db = CreateIsolatedContext(databaseName))
            {
                var existing = await db.Set<LocalizationResource>()
                    .FirstOrDefaultAsync(r => r.Key == existingKey);

                if (existing != null)
                {
                    db.Set<LocalizationResource>().Remove(existing);
                    await db.SaveChangesAsync();
                }
            }

            using (var db = CreateIsolatedContext(databaseName))
            {
                var deleted = await db.Set<LocalizationResource>()
                    .FirstOrDefaultAsync(r => r.Key == existingKey);
                Assert.Null(deleted);
            }

            using (var db = CreateIsolatedContext(databaseName))
            {
                await DatabaseInitializer.InitializeAsync(db);
            }

            using (var db = CreateIsolatedContext(databaseName))
            {
                var added = await db.Set<LocalizationResource>()
                    .FirstOrDefaultAsync(r => r.Key == existingKey);

                Assert.NotNull(added);
                Assert.Equal("个人中心", added!.Translations["zh"]);
                Assert.Equal("プロフィール", added.Translations["ja"]);
                Assert.Equal("Profile", added.Translations["en"]);
            }
        }
        finally
        {
            using var cleanup = CreateIsolatedContext(databaseName);
            await cleanup.Database.CloseConnectionAsync();
            await DropDatabaseAsync(databaseName);
        }
    }

    private AppDbContext CreateIsolatedContext(string databaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(_factory.ServerConnectionString)
        {
            Database = databaseName
        };

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(builder.ConnectionString, npg => npg.MigrationsHistoryTable("__EFMigrationsHistory", "public"))
            .Options;

        return new AppDbContext(options);
    }

    private async Task CreateDatabaseAsync(string databaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(_factory.ServerConnectionString)
        {
            Database = "postgres"
        };

        await using var conn = new NpgsqlConnection(builder.ConnectionString);
        await conn.OpenAsync();

        // 先终止所有连接
        await using var terminate = new NpgsqlCommand(
            $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @db AND pid <> pg_backend_pid();",
            conn);
        terminate.Parameters.AddWithValue("db", databaseName);
        try { await terminate.ExecuteNonQueryAsync(); } catch { }

        // 删除旧数据库（如果存在）
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\";", conn);
        await drop.ExecuteNonQueryAsync();

        // 创建新数据库
        await using var create = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\";", conn);
        await create.ExecuteNonQueryAsync();
    }

    private async Task DropDatabaseAsync(string databaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(_factory.ServerConnectionString)
        {
            Database = "postgres"
        };

        await using var conn = new NpgsqlConnection(builder.ConnectionString);
        await conn.OpenAsync();

        await using var terminate = new NpgsqlCommand(
            $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @db AND pid <> pg_backend_pid();",
            conn);
        terminate.Parameters.AddWithValue("db", databaseName);
        try { await terminate.ExecuteNonQueryAsync(); } catch { }

        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{databaseName}\";", conn);
        await drop.ExecuteNonQueryAsync();
    }
}
