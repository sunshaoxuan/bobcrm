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
        var list = JsonDocument.Parse(await listRes.Content.ReadAsStringAsync()).RootElement;
        Assert.True(list.GetArrayLength() >= 2);

        var firstId = list[0].GetProperty("id").GetInt32();
        var detailRes = await client.GetAsync($"/api/customers/{firstId}");
        detailRes.EnsureSuccessStatusCode();
        var detail = JsonDocument.Parse(await detailRes.Content.ReadAsStringAsync()).RootElement;
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
        var list = await client.GetFromJsonAsync<JsonElement>("/api/customers");
        Assert.True(list.GetArrayLength() > 0, "客户列表应该包含至少一个客户");
        var id = list[0].GetProperty("id").GetInt32();
        var detail = await client.GetFromJsonAsync<JsonElement>($"/api/customers/{id}");
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
        var payload = await ok.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(payload.GetProperty("newVersion").GetInt32() == version + 1);
    }
}
