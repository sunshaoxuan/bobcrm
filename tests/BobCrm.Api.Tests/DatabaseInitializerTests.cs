using BobCrm.Api.Abstractions;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.Sqlite;
using System.IO;

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

            // 验证所有必需的表和数据都已创建
            await AssertDatabaseIsFullyInitializedAsync(db);
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

            // 验证所有必需的表和数据都已创建
            await AssertDatabaseIsFullyInitializedAsync(db);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
            await DropDatabaseAsync(databaseName);
        }
    }

    /// <summary>
    /// 验证数据库已完全初始化，包含所有必需的表和种子数据
    /// </summary>
    private static async Task AssertDatabaseIsFullyInitializedAsync(AppDbContext db)
    {
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

    [Fact]
    public async Task Initialize_Removes_Runtime_Workflow_Entities()
    {
        var databaseName = $"dbinit_{Guid.NewGuid():N}";
        await CreateDatabaseAsync(databaseName);

        try
        {
            var workflowRoute = $"workflow_{Guid.NewGuid():N}";
            var workflowName = $"Workflow{Guid.NewGuid():N}";

            await using (var db = CreateIsolatedContext(databaseName))
            {
                await DatabaseInitializer.InitializeAsync(db);

                var runtimeEntity = new EntityDefinition
                {
                    Namespace = "BobCrm.Dynamic",
                    EntityName = workflowName,
                    EntityRoute = workflowRoute,
                    Source = EntitySource.Custom,
                    Status = EntityStatus.Published,
                    DisplayName = new Dictionary<string, string?>
                    {
                        ["zh"] = "Runtime Workflow",
                        ["en"] = "Runtime Workflow"
                    },
                    Fields = new List<FieldMetadata>(),
                    Interfaces = new List<EntityInterface>(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.EntityDefinitions.Add(runtimeEntity);
                await db.SaveChangesAsync();

                var template = new FormTemplate
                {
                    Name = "Workflow Detail",
                    EntityType = workflowRoute,
                    UserId = "__system__",
                    UsageType = FormTemplateUsageType.Detail,
                    LayoutJson = "[]",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.FormTemplates.Add(template);
                await db.SaveChangesAsync();

                var binding = new TemplateBinding
                {
                    EntityType = workflowRoute,
                    UsageType = FormTemplateUsageType.Detail,
                    TemplateId = template.Id,
                    IsSystem = true,
                    UpdatedBy = "tests",
                    UpdatedAt = DateTime.UtcNow
                };
                db.TemplateBindings.Add(binding);
                await db.SaveChangesAsync();

                var stateBinding = new TemplateStateBinding
                {
                    EntityType = workflowRoute,
                    TemplateId = template.Id,
                    ViewState = "DetailView",
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow
                };
                db.TemplateStateBindings.Add(stateBinding);
                await db.SaveChangesAsync();

                var menuNode = new FunctionNode
                {
                    Code = $"CRM.CORE.{workflowRoute.ToUpperInvariant()}",
                    Name = "Workflow Menu",
                    Route = $"/{workflowRoute}",
                    TemplateStateBindingId = stateBinding.Id,
                    DisplayName = new Dictionary<string, string?>
                    {
                        ["zh"] = "临时菜单",
                        ["en"] = "Temporary Menu"
                    }
                };
                db.FunctionNodes.Add(menuNode);
                await db.SaveChangesAsync();
            }

            // Run initialization again to trigger cleanup
            await using (var db = CreateIsolatedContext(databaseName))
            {
                await DatabaseInitializer.InitializeAsync(db);
            }

            await using (var db = CreateIsolatedContext(databaseName))
            {
                Assert.False(await db.EntityDefinitions.AnyAsync(ed => ed.Namespace == "BobCrm.Dynamic"));
                Assert.False(await db.FormTemplates.AnyAsync(t => t.EntityType == workflowRoute));
                Assert.False(await db.TemplateBindings.AnyAsync(b => b.EntityType == workflowRoute));
                var codes = await db.FunctionNodes.AsNoTracking().Select(fn => fn.Code).ToListAsync();
                Assert.DoesNotContain(codes, code => !string.IsNullOrEmpty(code) && code!.Contains(workflowRoute, StringComparison.OrdinalIgnoreCase));
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
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = GetDatabasePath(databaseName)
        };

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(builder.ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    private async Task CreateDatabaseAsync(string databaseName)
    {
        var dbPath = GetDatabasePath(databaseName);
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }

        await using var conn = new SqliteConnection($"Data Source={dbPath}");
        await conn.OpenAsync();
    }

    private Task DropDatabaseAsync(string databaseName)
    {
        var dbPath = GetDatabasePath(databaseName);
        try
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
        catch
        {
        }

        return Task.CompletedTask;
    }

    private static string GetDatabasePath(string databaseName)
        => Path.Combine(Path.GetTempPath(), $"{databaseName}.db");
}
