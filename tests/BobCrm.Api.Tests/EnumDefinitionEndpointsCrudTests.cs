using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace BobCrm.Api.Tests;

public class EnumDefinitionEndpointsCrudTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    private readonly HttpClient _client;

    public EnumDefinitionEndpointsCrudTests(TestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var (accessToken, _) = await client.LoginAsAdminAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    [Fact]
    public async Task EnumCrud_ShouldCreateGetUpdateDelete()
    {
        var client = await GetAuthenticatedClientAsync();

        var create = await client.PostAsJsonAsync("/api/enums", new
        {
            code = $"enum_{Guid.NewGuid():N}",
            displayName = new Dictionary<string, string?> { ["zh"] = "测试枚举", ["ja"] = "テスト", ["en"] = "Test" },
            description = new Dictionary<string, string?> { ["zh"] = "desc" },
            isEnabled = true
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = (await create.ReadAsJsonAsync()).UnwrapData();
        var id = created.GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(id));

        var get = await client.GetAsync($"/api/enums/{id}?lang=zh");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var update = await client.PutAsJsonAsync($"/api/enums/{id}", new
        {
            displayName = new Dictionary<string, string?> { ["zh"] = "测试枚举-更新", ["ja"] = "テスト", ["en"] = "Test" },
            description = new Dictionary<string, string?> { ["zh"] = "desc2" },
            isEnabled = true
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var del = await client.DeleteAsync($"/api/enums/{id}");
        Assert.Equal(HttpStatusCode.OK, del.StatusCode);

        var getAfter = await client.GetAsync($"/api/enums/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfter.StatusCode);
    }

    [Fact]
    public async Task EnumOptions_Update_ShouldReturnList()
    {
        var client = await GetAuthenticatedClientAsync();

        var create = await client.PostAsJsonAsync("/api/enums", new
        {
            code = $"enum_{Guid.NewGuid():N}",
            displayName = new Dictionary<string, string?> { ["zh"] = "测试枚举", ["ja"] = "テスト", ["en"] = "Test" },
            description = new Dictionary<string, string?>(),
            isEnabled = true,
            options = new object[]
            {
                new { value = "A", displayName = new Dictionary<string,string?> { ["zh"]="甲", ["ja"]="A", ["en"]="A" }, description = new Dictionary<string,string?>(), sortOrder = 1, colorTag = (string?)null, icon = (string?)null },
                new { value = "B", displayName = new Dictionary<string,string?> { ["zh"]="乙", ["ja"]="B", ["en"]="B" }, description = new Dictionary<string,string?>(), sortOrder = 2, colorTag = (string?)null, icon = (string?)null }
            }
        });
        create.EnsureSuccessStatusCode();
        var created = (await create.ReadAsJsonAsync()).UnwrapData();
        var id = created.GetProperty("id").GetString()!;

        // Create initial options
        var optionsResp = await client.GetAsync($"/api/enums/{id}/options");
        Assert.Equal(HttpStatusCode.OK, optionsResp.StatusCode);
        var optionsRoot = await optionsResp.ReadAsJsonAsync();
        var options = optionsRoot.UnwrapData();
        Assert.Equal(JsonValueKind.Array, options.ValueKind);

        var updateOptions = await client.PutAsJsonAsync($"/api/enums/{id}/options", new
        {
            options = new object[]
            {
                new { id = options[0].GetProperty("id").GetString(), displayName = new Dictionary<string,string?> { ["zh"]="甲", ["ja"]="A", ["en"]="A" }, description = new Dictionary<string,string?>(), sortOrder = 1, isEnabled = true },
                new { id = options.GetArrayLength() > 1 ? options[1].GetProperty("id").GetString() : options[0].GetProperty("id").GetString(), displayName = new Dictionary<string,string?> { ["zh"]="乙", ["ja"]="B", ["en"]="B" }, description = new Dictionary<string,string?>(), sortOrder = 2, isEnabled = true }
            }
        });
        Assert.Equal(HttpStatusCode.OK, updateOptions.StatusCode);
        var root = await updateOptions.ReadAsJsonAsync();
        var data = root.UnwrapData();
        Assert.Equal(JsonValueKind.Array, data.ValueKind);
        Assert.True(data.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task EnumEndpoints_WithoutAuth_ShouldReturn401()
    {
        var resp = await _client.GetAsync("/api/enums");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
