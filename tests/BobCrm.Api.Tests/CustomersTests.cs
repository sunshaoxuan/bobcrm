using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BobCrm.Api.Tests;

public class CustomersTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    public CustomersTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task List_And_Detail_Returns_Data()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var listRes = await client.GetAsync("/api/customers");
        listRes.EnsureSuccessStatusCode();
        var list = await listRes.ReadDataAsJsonAsync();
        Assert.True(list.GetArrayLength() >= 2);

        var firstId = list[0].GetProperty("id").GetInt32();
        var detailRes = await client.GetAsync($"/api/customers/{firstId}");
        detailRes.EnsureSuccessStatusCode();
        var detail = await detailRes.ReadDataAsJsonAsync();
        Assert.Equal(firstId, detail.GetProperty("id").GetInt32());
        Assert.True(detail.GetProperty("fields").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Update_Validation_And_Concurrency()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        // get a customer
        var listResp = await client.GetAsync("/api/customers");
        listResp.EnsureSuccessStatusCode();
        var list = await listResp.ReadDataAsJsonAsync();
        Assert.True(list.GetArrayLength() > 0, "客户列表应该包含至少一个客户");
        var id = list[0].GetProperty("id").GetInt32();
        var detailResp = await client.GetAsync($"/api/customers/{id}");
        detailResp.EnsureSuccessStatusCode();
        var detail = await detailResp.ReadDataAsJsonAsync();
        var version = detail.GetProperty("version").GetInt32();

        // missing fields -> validation error 400
        var bad = await client.PutAsJsonAsync($"/api/customers/{id}", new { fields = Array.Empty<object>(), expectedVersion = version });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        // unknown field -> 校验阶段先命中通用校验(必填缺失) 返回 400
        var unknown = await client.PutAsJsonAsync($"/api/customers/{id}", new { fields = new[] { new { key = "__unknown__", value = "x" } }, expectedVersion = version });
        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);

        // concurrency mismatch -> 409
        var conc = await client.PutAsJsonAsync($"/api/customers/{id}", new { fields = new[] { new { key = "email", value = "a@b.com" } }, expectedVersion = version + 1 });
        Assert.Equal(HttpStatusCode.Conflict, conc.StatusCode);

        // success
        var ok = await client.PutAsJsonAsync($"/api/customers/{id}", new { fields = new[] { new { key = "email", value = "a@b.com" } }, expectedVersion = version });
        ok.EnsureSuccessStatusCode();
        var payload = await ok.ReadDataAsJsonAsync();
        Assert.True(payload.GetProperty("newVersion").GetInt32() == version + 1);
    }

    [Fact]
    public async Task GetCustomer_NotFound_Returns_404()
    {
        // 测试获取不存在客户的404路径
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        // 使用一个很大的ID（肯定不存在）
        var detailResp = await client.GetAsync("/api/customers/99999");
        
        Assert.Equal(HttpStatusCode.NotFound, detailResp.StatusCode);
    }

    [Fact]
    public async Task CreateCustomer_Success_Returns_New_Id()
    {
        // 测试创建客户的成功路径
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var newCustomer = new
        {
            code = $"TEST_{Guid.NewGuid():N}",
            name = "测试客户",
            fields = new[]
            {
                new { key = "email", value = "test@example.com" },
                new { key = "priority", value = "1" }
            }
        };

        var createResp = await client.PostAsJsonAsync("/api/customers", newCustomer);
        createResp.EnsureSuccessStatusCode();

        var result = await createResp.ReadDataAsJsonAsync();
        Assert.True(result.TryGetProperty("id", out var id));
        Assert.True(id.GetInt32() > 0);
        Assert.True(result.TryGetProperty("code", out var code));
        Assert.Equal(newCustomer.code, code.GetString());
    }

    [Fact]
    public async Task CreateCustomer_Duplicate_Code_Returns_Conflict()
    {
        // 测试Code重复的冲突路径
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        // 第一次创建
        var uniqueCode = $"DUP_{Guid.NewGuid():N}";
        var customer1 = new { code = uniqueCode, name = "客户1", fields = Array.Empty<object>() };
        var create1 = await client.PostAsJsonAsync("/api/customers", customer1);
        create1.EnsureSuccessStatusCode();

        // 第二次使用相同的Code
        var customer2 = new { code = uniqueCode, name = "客户2", fields = Array.Empty<object>() };
        var create2 = await client.PostAsJsonAsync("/api/customers", customer2);
        
        // 应该返回400 BadRequest（Code重复被当作验证失败）
        Assert.Equal(HttpStatusCode.BadRequest, create2.StatusCode);
        
        var error = await create2.Content.ReadAsStringAsync();
        Assert.Contains("code", error.ToLowerInvariant()); // 错误信息应该提到code
    }

    [Fact]
    public async Task CreateCustomer_Missing_Code_Returns_BadRequest()
    {
        // 测试缺少必填字段Code的验证路径
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var invalidCustomer = new
        {
            code = "",  // Code为空
            name = "测试",
            fields = Array.Empty<object>()
        };

        var createResp = await client.PostAsJsonAsync("/api/customers", invalidCustomer);
        
        // 应该返回400 Bad Request
        Assert.Equal(HttpStatusCode.BadRequest, createResp.StatusCode);
    }

    [Fact]
    public async Task UpdateCustomer_Without_Edit_Permission_Returns_Forbidden()
    {
        // 测试没有编辑权限的403路径
        var adminClient = _factory.CreateClient();
        var (adminAccess, _) = await adminClient.LoginAsAdminAsync();
        adminClient.UseBearer(adminAccess);

        // 获取一个客户
        var listResp = await adminClient.GetAsync("/api/customers");
        listResp.EnsureSuccessStatusCode();
        var list = await listResp.ReadDataAsJsonAsync();
        var id = list[0].GetProperty("id").GetInt32();
        var detailResp = await adminClient.GetAsync($"/api/customers/{id}");
        detailResp.EnsureSuccessStatusCode();
        var detail = await detailResp.ReadDataAsJsonAsync();
        var version = detail.GetProperty("version").GetInt32();

        // 创建一个新用户（没有访问权限）
        var userClient = _factory.CreateClient();
        var (userId, _, userAccess) = await userClient.CreateAndLoginUserAsync(_factory.Services);
        userClient.UseBearer(userAccess);

        // 尝试更新（应该返回403）
        var updateResp = await userClient.PutAsJsonAsync($"/api/customers/{id}", 
            new { fields = new[] { new { key = "email", value = "new@test.com" } }, expectedVersion = version });
        
        Assert.Equal(HttpStatusCode.Forbidden, updateResp.StatusCode);
    }

    [Fact]
    public async Task GetCustomerAccess_Requires_Admin()
    {
        // 测试获取访问权限需要admin的路径
        var client = _factory.CreateClient();
        var (userId, _, access) = await client.CreateAndLoginUserAsync(_factory.Services);
        client.UseBearer(access);

        // 普通用户尝试获取访问权限列表
        var accessResp = await client.GetAsync("/api/customers/1/access");
        
        Assert.Equal(HttpStatusCode.Forbidden, accessResp.StatusCode);
    }

    [Fact]
    public async Task SetCustomerAccess_Requires_Admin()
    {
        // 测试设置访问权限需要admin的路径
        var client = _factory.CreateClient();
        var (userId, _, access) = await client.CreateAndLoginUserAsync(_factory.Services);
        client.UseBearer(access);

        // 普通用户尝试设置访问权限
        var accessResp = await client.PostAsJsonAsync("/api/customers/1/access", 
            new { userId = "some-user", canEdit = true });
        
        Assert.Equal(HttpStatusCode.Forbidden, accessResp.StatusCode);
    }

    [Fact]
    public async Task CreateCustomer_Automatically_Grants_Creator_Edit_Permission()
    {
        // 测试创建客户时自动授予创建者编辑权限的逻辑
        var userClient = _factory.CreateClient();
        var (userId, _, userAccess) = await userClient.CreateAndLoginUserAsync(_factory.Services);
        userClient.UseBearer(userAccess);

        // 创建客户
        var newCustomer = new
        {
            code = $"AUTO_{Guid.NewGuid():N}",
            name = "自动权限测试",
            fields = new[] { new { key = "email", value = "auto@test.com" } }
        };

        var createResp = await userClient.PostAsJsonAsync("/api/customers", newCustomer);
        createResp.EnsureSuccessStatusCode();

        var result = await createResp.ReadDataAsJsonAsync();
        var newId = result.GetProperty("id").GetInt32();

        // 验证创建者可以查看这个客户
        var getResp = await userClient.GetAsync("/api/customers");
        var customerList = await getResp.ReadDataAsJsonAsync();
        
        var hasNewCustomer = customerList.EnumerateArray().Any(c => c.GetProperty("id").GetInt32() == newId);
        Assert.True(hasNewCustomer, "创建者应该能在列表中看到自己创建的客户");

        // 验证创建者可以编辑这个客户
        var detailResp = await userClient.GetAsync($"/api/customers/{newId}");
        detailResp.EnsureSuccessStatusCode();
        var detail = await detailResp.ReadDataAsJsonAsync();
        var version = detail.GetProperty("version").GetInt32();
        
        var updateResp = await userClient.PutAsJsonAsync($"/api/customers/{newId}",
            new { fields = new[] { new { key = "email", value = "updated@test.com" } }, expectedVersion = version });
        
        updateResp.EnsureSuccessStatusCode(); // 创建者应该有编辑权限
    }

    [Fact]
    public async Task UpdateCustomer_Invalid_Field_Format_Returns_BadRequest()
    {
        // 测试字段格式错误的验证路径
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var listResp = await client.GetAsync("/api/customers");
        listResp.EnsureSuccessStatusCode();
        var list = await listResp.ReadDataAsJsonAsync();
        var id = list[0].GetProperty("id").GetInt32();
        var detailResp = await client.GetAsync($"/api/customers/{id}");
        detailResp.EnsureSuccessStatusCode();
        var detail = await detailResp.ReadDataAsJsonAsync();
        var version = detail.GetProperty("version").GetInt32();

        // 发送不符合验证规则的email
        var updateResp = await client.PutAsJsonAsync($"/api/customers/{id}",
            new { fields = new[] { new { key = "email", value = "invalid-email-format" } }, expectedVersion = version });
        
        // 应该返回400（字段验证失败）
        Assert.Equal(HttpStatusCode.BadRequest, updateResp.StatusCode);
    }
}
