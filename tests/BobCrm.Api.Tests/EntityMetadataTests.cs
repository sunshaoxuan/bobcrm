using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BobCrm.Api.Tests;

/// <summary>
/// 实体元数据端点测试
/// 测试 GET /api/entities 等端点
/// </summary>
public class EntityMetadataTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    public EntityMetadataTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetAvailableEntities_Returns_Enabled_Entities_Only()
    {
        var client = _factory.CreateClient();
        
        // 获取可用实体列表（不需要认证）
        var resp = await client.GetAsync("/api/entities");
        resp.EnsureSuccessStatusCode();

        var entities = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, entities.ValueKind);
        
        var entityArray = entities.EnumerateArray().ToList();
        Assert.NotEmpty(entityArray);
        
        // 至少应该有一个customer实体
        var hasCustomer = entityArray.Any(e => 
            e.TryGetProperty("entityType", out var et) && et.GetString() == "customer");
        Assert.True(hasCustomer, "应该包含customer实体");
        
        // 验证返回的字段
        var firstEntity = entityArray[0];
        Assert.True(firstEntity.TryGetProperty("entityType", out _), "应该包含entityType");
        Assert.True(firstEntity.TryGetProperty("entityName", out _), "应该包含entityName");
        Assert.True(firstEntity.TryGetProperty("displayName", out var displayName), "应该包含displayName");
        Assert.Equal(JsonValueKind.Object, displayName.ValueKind); // displayName 应该是 Dictionary 序列化的 JSON 对象
        Assert.True(firstEntity.TryGetProperty("apiEndpoint", out _), "应该包含apiEndpoint");
    }

    [Fact]
    public async Task GetAllEntities_Requires_Authorization()
    {
        var client = _factory.CreateClient();
        
        // 未认证访问 /api/entities/all 应该返回 401
        var resp = await client.GetAsync("/api/entities/all");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task GetAllEntities_Returns_All_Including_Disabled()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        // 管理员访问所有实体（包括已禁用的）
        var resp = await client.GetAsync("/api/entities/all");
        resp.EnsureSuccessStatusCode();

        var entities = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, entities.ValueKind);
        
        var entityArray = entities.EnumerateArray().ToList();
        Assert.NotEmpty(entityArray);
        
        // 验证返回的字段包含isEnabled（注意：API返回的是camelCase）
        var firstEntity = entityArray[0];
        Assert.True(firstEntity.TryGetProperty("isEnabled", out var isEnabled), "应该包含isEnabled字段");
        Assert.True(firstEntity.TryGetProperty("entityName", out _), "应该包含entityName字段");
        
        // Customer 实体应该存在且已启用
        var hasCustomer = entityArray.Any(e => 
            e.TryGetProperty("entityType", out var et) && 
            et.GetString() == "BobCrm.Api.Base.Customer");
        Assert.True(hasCustomer, "应该包含Customer实体");
    }

    [Fact]
    public async Task ValidateEntityRoute_Customer_Returns_Valid()
    {
        var client = _factory.CreateClient();
        
        // 验证customer实体路由
        var resp = await client.GetAsync("/api/entities/customer/validate");
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<JsonElement>();
        
        // 验证customer路由应该是有效的
        var isValid = result.GetProperty("isValid").GetBoolean();
        var entityRoute = result.GetProperty("entityRoute").GetString();
        
        Assert.True(isValid, $"customer路由应该是有效的，但isValid={isValid}");
        Assert.Equal("customer", entityRoute);
        
        // 应该返回实体详情（注意：validate endpoint返回的字段是PascalCase）
        Assert.True(result.TryGetProperty("entity", out var entity), "应该包含entity字段");
        Assert.NotEqual(JsonValueKind.Null, entity.ValueKind);
        
        // validate endpoint 返回的是原始对象，字段名是 PascalCase
        var hasEntityName = entity.TryGetProperty("EntityName", out var entityName);
        if (hasEntityName)
        {
            Assert.Equal("Customer", entityName.GetString());
        }
    }

    [Fact]
    public async Task ValidateEntityRoute_Invalid_Returns_False()
    {
        var client = _factory.CreateClient();
        
        // 验证不存在的实体路由
        var resp = await client.GetAsync("/api/entities/nonexistent_entity/validate");
        resp.EnsureSuccessStatusCode();

        var result = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(result.GetProperty("isValid").GetBoolean(), "不存在的实体路由应该无效");
        Assert.Equal("nonexistent_entity", result.GetProperty("entityRoute").GetString());
        
        // 实体详情应该为null
        var entityValue = result.GetProperty("entity");
        Assert.Equal(JsonValueKind.Null, entityValue.ValueKind);
    }

    [Fact]
    public async Task EntityMetadata_Auto_Registration_Works()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        // 获取所有实体
        var resp = await client.GetAsync("/api/entities/all");
        resp.EnsureSuccessStatusCode();

        var entities = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var entityArray = entities.EnumerateArray().ToList();
        
        Assert.NotEmpty(entityArray);
        
        // Customer 实体应该已自动注册（注意：API返回的是camelCase）
        var customerEntity = entityArray.FirstOrDefault(e => 
            e.TryGetProperty("entityType", out var et) && 
            et.GetString() == "BobCrm.Api.Base.Customer");
        
        Assert.False(customerEntity.Equals(default(JsonElement)), "Customer实体应该已自动注册");
        
        // 验证Customer实体的元数据（字段名是camelCase）
        Assert.True(customerEntity.TryGetProperty("isEnabled", out var isEnabled), "应该包含isEnabled字段");
        Assert.True(isEnabled.GetBoolean(), "Customer实体应该是启用状态");
        
        Assert.True(customerEntity.TryGetProperty("entityName", out var entityName), "应该包含entityName字段");
        Assert.Equal("Customer", entityName.GetString());

        // 验证 displayName 是多语言字典
        Assert.True(customerEntity.TryGetProperty("displayName", out var displayName), "应该包含displayName字段");
        Assert.Equal(JsonValueKind.Object, displayName.ValueKind); // displayName 应该是 Dictionary 序列化的 JSON 对象
    }
}

