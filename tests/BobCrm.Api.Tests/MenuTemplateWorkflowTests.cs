using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace BobCrm.Api.Tests;

public class MenuTemplateWorkflowTests : IClassFixture<MenuWorkflowAppFactory>
{
    private readonly MenuWorkflowAppFactory _factory;

    public MenuTemplateWorkflowTests(MenuWorkflowAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Published_entity_menu_role_and_template_flow_is_reported_by_api()
    {
        var adminClient = _factory.CreateClient();
        var (adminAccess, _) = await adminClient.LoginAsAdminAsync();
        adminClient.UseBearer(adminAccess);

        var manageResponse = await adminClient.GetAsync("/api/access/functions/manage");
        manageResponse.EnsureSuccessStatusCode();
        using var manageDoc = JsonDocument.Parse(await manageResponse.Content.ReadAsStringAsync());
        Assert.True(TryFindFunctionNode(manageDoc.RootElement, "CRM.CORE.ACCOUNTS", out var accountsNode));
        var translations = accountsNode.GetProperty("displayNameTranslations");
        Assert.Equal("Customer Accounts", translations.GetProperty("en").GetString());
        Assert.Equal("顧客マスタ", translations.GetProperty("ja").GetString());

        var entity = await SeedDraftEntityAsync();

        var publishResponse = await adminClient.PostAsync($"/api/entity-definitions/{entity.Id}/publish", null);
        publishResponse.EnsureSuccessStatusCode();
        using var publishDoc = JsonDocument.Parse(await publishResponse.Content.ReadAsStringAsync());
        var publishRoot = publishDoc.RootElement;
        Assert.True(publishRoot.GetProperty("success").GetBoolean());

        var menuNodes = publishRoot.GetProperty("menus").EnumerateArray().ToList();
        Assert.NotEmpty(menuNodes);
        var listNode = menuNodes.First(n => n.GetProperty("usage").GetInt32() == (int)FormTemplateUsageType.List);
        var detailNode = menuNodes.First(n => n.GetProperty("usage").GetInt32() == (int)FormTemplateUsageType.Detail);
        var menuNodeIds = new[]
        {
            listNode.GetProperty("nodeId").GetGuid(),
            detailNode.GetProperty("nodeId").GetGuid()
        };

        var bindings = publishRoot.GetProperty("bindings").EnumerateArray().ToList();
        Assert.Contains(bindings, b => b.GetProperty("usage").GetInt32() == (int)FormTemplateUsageType.Detail);

        var roleResponse = await adminClient.PostAsJsonAsync("/api/access/roles", new
        {
            code = $"ROLE.WF.{Guid.NewGuid():N}".Substring(0, 12),
            name = "Workflow Access",
            description = "integration workflow test",
            isEnabled = true,
            functionIds = menuNodeIds
        });
        roleResponse.EnsureSuccessStatusCode();
        using var roleDoc = JsonDocument.Parse(await roleResponse.Content.ReadAsStringAsync());
        var roleId = roleDoc.RootElement.GetProperty("id").GetGuid();

        var userClient = _factory.CreateClient();
        var (userId, _, userAccess) = await userClient.CreateAndLoginUserAsync(_factory.Services);
        userClient.UseBearer(userAccess);

        var initialBindings = await userClient.GetAsync("/api/templates/menu-bindings?usageType=Detail");
        initialBindings.EnsureSuccessStatusCode();
        using var emptyDoc = JsonDocument.Parse(await initialBindings.Content.ReadAsStringAsync());
        Assert.Equal(0, emptyDoc.RootElement.GetArrayLength());

        var assignResponse = await adminClient.PostAsJsonAsync("/api/access/assignments", new
        {
            userId,
            roleId,
            organizationId = (Guid?)null
        });
        assignResponse.EnsureSuccessStatusCode();

        var menuBindingResponse = await userClient.GetAsync("/api/templates/menu-bindings?usageType=Detail");
        menuBindingResponse.EnsureSuccessStatusCode();
        using var bindingDoc = JsonDocument.Parse(await menuBindingResponse.Content.ReadAsStringAsync());
        var items = bindingDoc.RootElement.EnumerateArray().ToList();
        Assert.NotEmpty(items);
        var workflowEntry = items.First(entry =>
            entry.GetProperty("Binding").GetProperty("entityType").GetString() == entity.EntityRoute);

        var menuInfo = workflowEntry.GetProperty("Menu");
        Assert.Equal($"CRM.CORE.{entity.EntityRoute.ToUpperInvariant()}", menuInfo.GetProperty("code").GetString());
        Assert.Equal(entity.DisplayName!["zh"], menuInfo.GetProperty("name").GetString());
        var expectedRoute = entity.ApiEndpoint.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
            ? entity.ApiEndpoint[4..]
            : entity.ApiEndpoint.TrimStart('/');
        Assert.Equal(expectedRoute, menuInfo.GetProperty("route").GetString());

        var bindingInfo = workflowEntry.GetProperty("Binding");
        Assert.Equal((int)FormTemplateUsageType.Detail, bindingInfo.GetProperty("usageType").GetInt32());

        var templateList = workflowEntry.GetProperty("Templates");
        Assert.True(templateList.GetArrayLength() > 0);
        Assert.Contains(templateList.EnumerateArray(), t => t.GetProperty("isSystemDefault").GetBoolean());
    }

    private async Task<EntityDefinition> SeedDraftEntityAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entityName = $"Workflow{Guid.NewGuid():N}".Substring(0, 8);
        var entityRoute = $"wf_{Guid.NewGuid():N}".Substring(0, 8).ToLowerInvariant();

        var entity = new EntityDefinition
        {
            Namespace = "BobCrm.Dynamic",
            EntityName = entityName,
            FullTypeName = $"BobCrm.Dynamic.{entityName}",
            EntityRoute = entityRoute,
            DisplayName = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["zh"] = "运行态菜单",
                ["en"] = "Runtime Menu"
            },
            Description = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["zh"] = "集成测试实体"
            },
            ApiEndpoint = $"/api/{entityRoute}",
            StructureType = EntityStructureType.Single,
            Category = "CRM",
            Status = EntityStatus.Draft,
            Order = 1,
            Icon = "appstore",
            CreatedBy = "admin",
            UpdatedBy = "admin"
        };

        entity.Fields.Add(new FieldMetadata
        {
            PropertyName = "Title",
            DisplayName = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["zh"] = "标题",
                ["en"] = "Title"
            },
            DataType = FieldDataType.String,
            Length = 200,
            IsRequired = true,
            SortOrder = 10
        });

        await db.EntityDefinitions.AddAsync(entity);
        await db.SaveChangesAsync();
        return entity;
    }

    private static bool TryFindFunctionNode(JsonElement element, string code, out JsonElement node)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
            {
                if (TryFindFunctionNode(child, code, out node))
                {
                    return true;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("code", out var codeProperty) &&
                string.Equals(codeProperty.GetString(), code, StringComparison.Ordinal))
            {
                node = element;
                return true;
            }

            if (element.TryGetProperty("children", out var children) && children.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in children.EnumerateArray())
                {
                    if (TryFindFunctionNode(child, code, out node))
                    {
                        return true;
                    }
                }
            }
        }

        node = default;
        return false;
    }
}

public sealed class MenuWorkflowAppFactory : WebApplicationFactory<Program>
{
    private readonly string _serverConnectionString =
        "Host=localhost;Port=5432;Username=postgres;Password=postgres";

    private readonly string _databaseName = $"bobcrm_test_{Guid.NewGuid():N}";

    public string ServerConnectionString => _serverConnectionString;
    public string DefaultDatabaseName => _databaseName;
    public string DefaultConnectionString => $"{_serverConnectionString};Database={_databaseName}";

    private static readonly SemaphoreSlim HostSemaphore = new(1, 1);
    private bool _lockHeld;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        HostSemaphore.Wait();
        _lockHeld = true;

        builder.UseEnvironment("Development");

        // 强制使用测试数据库配置
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.Sources.Clear();

            var testConfig = new Dictionary<string, string?>
            {
                ["Db:Provider"] = "postgres",
                ["ConnectionStrings:Default"] = DefaultConnectionString,
                ["Db:SkipInit"] = "true",
                ["Jwt:Key"] = "dev-secret-change-in-prod-1234567890",
                ["Jwt:Issuer"] = "BobCrm",
                ["Jwt:Audience"] = "BobCrmUsers",
                ["Jwt:AccessMinutes"] = "60",
                ["Jwt:RefreshDays"] = "7"
            };

            config.AddInMemoryCollection(testConfig!);
        });

        // 在启动应用程序之前初始化数据库
        var connectionString = DefaultConnectionString;

        // 步骤1：强制终止所有连接并删除/创建数据库
        var connBuilder = new NpgsqlConnectionStringBuilder(connectionString) { Database = "postgres" };
        using (var adminConn = new NpgsqlConnection(connBuilder.ToString()))
        {
            adminConn.Open();

            using (var cmd = new NpgsqlCommand(
                $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{_databaseName}' AND pid <> pg_backend_pid();",
                adminConn))
            {
                try { cmd.ExecuteNonQuery(); } catch { }
            }
            using (var cmd = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\";", adminConn))
            {
                cmd.ExecuteNonQuery();
            }
            using (var cmd = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\";", adminConn))
            {
                cmd.ExecuteNonQuery();
            }
        }

        // 步骤2：应用迁移并初始化数据
        var testDbOptions = new DbContextOptionsBuilder<AppDbContext>();
        testDbOptions.UseNpgsql(connectionString);
        using (var tempDb = new AppDbContext(testDbOptions.Options))
        {
            DatabaseInitializer.InitializeAsync(tempDb).GetAwaiter().GetResult();
        }

        IHost host;
        try
        {
            host = base.CreateHost(builder);
        }
        catch
        {
            ReleaseHostLock();
            throw;
        }

        // Seed additional test-specific data
        using (var scope = host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Seed MENU_CRM_CORE_ACCOUNTS localization
            SeedLocalizationAsync(db).GetAwaiter().GetResult();

            var accessService = scope.ServiceProvider.GetRequiredService<AccessService>();
            accessService.SeedSystemAdministratorAsync().GetAwaiter().GetResult();
        }

        return host;
    }

    private void ReleaseHostLock()
    {
        if (_lockHeld)
        {
            HostSemaphore.Release();
            _lockHeld = false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            DropDatabase();
        }
        finally
        {
            ReleaseHostLock();
        }
    }

    private void DropDatabase()
    {
        try
        {
            var builder = new NpgsqlConnectionStringBuilder(ServerConnectionString)
            {
                Database = "postgres"
            };

            using var adminConn = new NpgsqlConnection(builder.ConnectionString);
            adminConn.Open();

            using (var terminate = new NpgsqlCommand(
                       $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{_databaseName}' AND pid <> pg_backend_pid();",
                       adminConn))
            {
                try { terminate.ExecuteNonQuery(); } catch { }
            }

            using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\";", adminConn);
            drop.ExecuteNonQuery();
        }
        catch
        {
        }
    }

    private static async Task SeedLocalizationAsync(AppDbContext db)
    {
        const string key = "MENU_CRM_CORE_ACCOUNTS";
        var resource = await db.LocalizationResources.FirstOrDefaultAsync(r => r.Key == key);
        if (resource == null)
        {
            resource = new LocalizationResource
            {
                Key = key,
                Translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["zh"] = "客户主档",
                    ["en"] = "Customer Accounts",
                    ["ja"] = "顧客マスタ"
                }
            };
            db.LocalizationResources.Add(resource);
        }
        else
        {
            resource.Translations["zh"] = "客户主档";
            resource.Translations["en"] = "Customer Accounts";
            resource.Translations["ja"] = "顧客マスタ";
        }

        await db.SaveChangesAsync();
    }
}

internal sealed class TestFriendlyDDLExecutionService : DDLExecutionService
{
    public TestFriendlyDDLExecutionService(AppDbContext db, ILogger<DDLExecutionService> logger)
        : base(db, logger)
    {
    }

    public override async Task<DDLScript> ExecuteDDLAsync(
        Guid entityDefinitionId,
        string scriptType,
        string sqlScript,
        string? createdBy = null)
    {
        var script = new DDLScript
        {
            EntityDefinitionId = entityDefinitionId,
            ScriptType = scriptType,
            SqlScript = sqlScript,
            Status = DDLScriptStatus.Success,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
            ExecutedAt = DateTime.UtcNow
        };

        await _db.DDLScripts.AddAsync(script);
        await _db.SaveChangesAsync();
        return script;
    }

    public override async Task<List<DDLScript>> ExecuteDDLBatchAsync(
        Guid entityDefinitionId,
        List<(string ScriptType, string SqlScript)> scripts,
        string? createdBy = null)
    {
        var results = new List<DDLScript>();
        foreach (var (scriptType, sqlScript) in scripts)
        {
            var result = await ExecuteDDLAsync(entityDefinitionId, scriptType, sqlScript, createdBy);
            results.Add(result);
        }

        return results;
    }

    public override Task<bool> TableExistsAsync(string tableName)
        => Task.FromResult(false);

    public override Task<List<TableColumnInfo>> GetTableColumnsAsync(string tableName)
        => Task.FromResult(new List<TableColumnInfo>());
}
