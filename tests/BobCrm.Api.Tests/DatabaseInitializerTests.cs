using BobCrm.Api.Abstractions;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BobCrm.Api.Tests;

/// <summary>
/// 数据库初始化逻辑测试 - 特别关注实体自动注册的各种逻辑分支
/// </summary>
public class DatabaseInitializerTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    public DatabaseInitializerTests(TestWebAppFactory factory) => _factory = factory;

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
        Assert.Contains("BobCrm.Api.Domain.Customer", json);
        Assert.Contains("ENTITY_CUSTOMER", json);
        Assert.Contains("customer", json); // entityRoute
        Assert.Contains("Customer", json); // entityName
    }

    [Fact]
    public async Task AutoRegister_Re_Enables_Previously_Disabled_Entity()
    {
        // 这个测试验证：已禁用实体的重新启用路径
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // 手动禁用Customer实体
        var customer = await db.EntityMetadata
            .FirstOrDefaultAsync(e => e.EntityType == "BobCrm.Api.Domain.Customer");
        
        if (customer != null)
        {
            customer.IsEnabled = false;
            await db.SaveChangesAsync();
            
            // 验证已禁用
            Assert.False(customer.IsEnabled);
            
            // 再次调用初始化（会触发自动注册逻辑）
            await DatabaseInitializer.InitializeAsync(db);
            
            // 刷新实体
            await db.Entry(customer).ReloadAsync();
            
            // 验证已重新启用
            Assert.True(customer.IsEnabled, "之前禁用的Customer实体应该被重新启用");
            Assert.NotNull(customer.UpdatedAt);
        }
    }

    [Fact]
    public async Task AutoRegister_Deactivates_Nonexistent_Entity_Metadata()
    {
        // 这个测试验证：反向验证失效路径
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // 手动插入一个不存在的实体元数据
        var fakeEntity = new Data.Entities.EntityMetadata
        {
            EntityType = "BobCrm.Api.Domain.NonExistentEntity",  // 这个类不存在
            EntityName = "NonExistentEntity",
            EntityRoute = "nonexistent",
            DisplayNameKey = "ENTITY_NONEXISTENT",
            ApiEndpoint = "/api/nonexistent",
            IsRootEntity = true,
            IsEnabled = true,  // 初始启用
            Order = 999,
            CreatedAt = DateTime.UtcNow
        };
        
        db.EntityMetadata.Add(fakeEntity);
        await db.SaveChangesAsync();
        
        // 验证已插入且启用
        var inserted = await db.EntityMetadata.FindAsync("BobCrm.Api.Domain.NonExistentEntity");
        Assert.NotNull(inserted);
        Assert.True(inserted.IsEnabled);
        
        // 再次调用初始化（会触发反向验证逻辑）
        await DatabaseInitializer.InitializeAsync(db);
        
        // 刷新实体
        await db.Entry(inserted).ReloadAsync();
        
        // 验证已失效（因为对应的类不存在）
        Assert.False(inserted.IsEnabled, "不存在的实体类应该被标记为失效");
    }

    [Fact]
    public async Task AutoRegister_Skips_Already_Enabled_Entity()
    {
        // 这个测试验证：已存在且启用的实体的跳过路径
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // 获取Customer实体的当前UpdatedAt
        var customer = await db.EntityMetadata
            .FirstOrDefaultAsync(e => e.EntityType == "BobCrm.Api.Domain.Customer");
        
        Assert.NotNull(customer);
        Assert.True(customer.IsEnabled);
        
        var originalUpdatedAt = customer.UpdatedAt;
        
        // 再次调用初始化
        await DatabaseInitializer.InitializeAsync(db);
        
        // 刷新实体
        await db.Entry(customer).ReloadAsync();
        
        // 验证UpdatedAt没有变化（说明跳过了更新）
        Assert.Equal(originalUpdatedAt, customer.UpdatedAt);
        Assert.True(customer.IsEnabled);
    }

    [Fact]
    public async Task Initialize_Creates_All_Required_Tables_And_Data()
    {
        // 这个测试验证：完整的初始化流程
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // 验证所有必要的数据都已初始化
        
        // 1. Customers
        var customersExist = await db.Set<Domain.Customer>().AnyAsync();
        Assert.True(customersExist, "应该有初始客户数据");
        
        // 2. FieldDefinitions
        var fieldsExist = await db.Set<Domain.FieldDefinition>().AnyAsync();
        Assert.True(fieldsExist, "应该有字段定义");
        
        // 3. LocalizationLanguages
        var langsExist = await db.Set<Domain.LocalizationLanguage>().AnyAsync();
        Assert.True(langsExist, "应该有语言配置");
        
        // 4. LocalizationResources
        var resourcesExist = await db.Set<Domain.LocalizationResource>().AnyAsync();
        Assert.True(resourcesExist, "应该有多语言资源");
        
        // 5. EntityMetadata
        var entitiesExist = await db.Set<Data.Entities.EntityMetadata>().AnyAsync();
        Assert.True(entitiesExist, "应该有实体元数据");
        
        // 6. UserLayouts (default template)
        var defaultLayout = await db.Set<Domain.UserLayout>()
            .FirstOrDefaultAsync(l => l.UserId == "__default__" && l.CustomerId == 0);
        Assert.NotNull(defaultLayout);
        Assert.False(string.IsNullOrWhiteSpace(defaultLayout.LayoutJson));
    }

    [Fact]
    public async Task Initialize_Ensure_Method_Adds_Missing_Keys()
    {
        // 这个测试验证：Ensure方法添加缺失键值的逻辑
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // 删除一个Ensure会添加的资源（如果存在）
        var existingKey = "MENU_PROFILE";
        var existing = await db.Set<Domain.LocalizationResource>()
            .FirstOrDefaultAsync(r => r.Key == existingKey);
        
        if (existing != null)
        {
            db.Set<Domain.LocalizationResource>().Remove(existing);
            await db.SaveChangesAsync();
        }
        
        // 验证资源不存在
        var deleted = await db.Set<Domain.LocalizationResource>()
            .FirstOrDefaultAsync(r => r.Key == existingKey);
        Assert.Null(deleted);
        
        // 再次初始化（应该通过Ensure添加缺失的键）
        await DatabaseInitializer.InitializeAsync(db);
        
        // 验证资源已被添加
        var added = await db.Set<Domain.LocalizationResource>()
            .FirstOrDefaultAsync(r => r.Key == existingKey);
        
        Assert.NotNull(added);
        Assert.Equal("个人中心", added.ZH);
        Assert.Equal("プロフィール", added.JA);
        Assert.Equal("Profile", added.EN);
    }
}

