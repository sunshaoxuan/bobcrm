using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BobCrm.Api.Contracts.DTOs.Access;
using BobCrm.Api.Contracts.Requests.Access;
using Xunit;

namespace BobCrm.Api.Tests;

public class AccessEndpointsTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public AccessEndpointsTests(TestWebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);
        return client;
    }

    [Fact]
    public async Task GetFunctions_WithoutLang_ReturnsTranslationsMode()
    {
        var client = await CreateAuthenticatedClientAsync();
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));

        var response = await client.GetAsync("/api/access/functions");
        response.EnsureSuccessStatusCode();

        var root = await response.ReadDataAsJsonAsync();
        Assert.Equal(JsonValueKind.Array, root.ValueKind);

        AssertTreeLanguageMode(root, expectedSingleLanguage: false);
        Assert.True(TryFindNodeByCode(root, "SYS.SET.MENU", out var node));
        Assert.False(node.TryGetProperty("displayName", out _));
        Assert.True(node.TryGetProperty("displayNameTranslations", out _));
    }

    [Fact]
    public async Task GetFunctions_WithLang_ReturnsSingleLanguageMode()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/access/functions?lang=ja");
        response.EnsureSuccessStatusCode();

        var root = await response.ReadDataAsJsonAsync();

        AssertTreeLanguageMode(root, expectedSingleLanguage: true);
        Assert.True(TryFindNodeByCode(root, "SYS.SET.MENU", out var node));
        Assert.True(node.TryGetProperty("displayName", out var displayName));
        Assert.Equal("メニュー管理", displayName.GetString());
        Assert.False(node.TryGetProperty("displayNameTranslations", out _));
    }

    [Fact]
    public async Task GetFunctionsManage_WithoutLang_ReturnsTranslationsMode()
    {
        var client = await CreateAuthenticatedClientAsync();
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("ja"));

        var response = await client.GetAsync("/api/access/functions/manage");
        response.EnsureSuccessStatusCode();

        var root = await response.ReadDataAsJsonAsync();

        AssertTreeLanguageMode(root, expectedSingleLanguage: false);
        Assert.True(TryFindNodeByCode(root, "SYS.SET.MENU", out var node));
        Assert.False(node.TryGetProperty("displayName", out _));
        Assert.True(node.TryGetProperty("displayNameTranslations", out _));
    }

    [Fact]
    public async Task GetFunctionsManage_WithLang_ReturnsSingleLanguageMode()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/access/functions/manage?lang=en");
        response.EnsureSuccessStatusCode();

        var root = await response.ReadDataAsJsonAsync();

        AssertTreeLanguageMode(root, expectedSingleLanguage: true);
        Assert.True(TryFindNodeByCode(root, "SYS.SET.MENU", out var node));
        Assert.True(node.TryGetProperty("displayName", out var displayName));
        Assert.Equal("Menu Management", displayName.GetString());
        Assert.False(node.TryGetProperty("displayNameTranslations", out _));
    }

    [Fact]
    public async Task CreateFunction_WithLang_ReturnsSingleLanguageMode()
    {
        var client = await CreateAuthenticatedClientAsync();
        var code = $"TEST.FUNC.CREATE.{Guid.NewGuid():N}";

        var request = new CreateFunctionRequest
        {
            ParentId = null,
            Code = code,
            Name = "Test Function",
            DisplayName = new()
            {
                ["zh"] = "测试功能",
                ["ja"] = "テスト機能",
                ["en"] = "Test Function"
            },
            Route = "/test/function",
            Icon = "test",
            IsMenu = true,
            SortOrder = 999
        };

        var response = await client.PostAsJsonAsync("/api/access/functions?lang=zh", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var root = await response.ReadDataAsJsonAsync();

        Assert.True(root.TryGetProperty("id", out _));
        Assert.True(root.TryGetProperty("displayName", out var displayName));
        Assert.Equal("测试功能", displayName.GetString());
        Assert.False(root.TryGetProperty("displayNameTranslations", out _));
    }

    [Fact]
    public async Task UpdateFunction_WithLang_ReturnsSingleLanguageMode()
    {
        var client = await CreateAuthenticatedClientAsync();
        var code = $"TEST.FUNC.UPDATE.{Guid.NewGuid():N}";

        var create = new CreateFunctionRequest
        {
            ParentId = null,
            Code = code,
            Name = "Test Function For Update",
            DisplayName = new()
            {
                ["zh"] = "更新前",
                ["ja"] = "更新前",
                ["en"] = "Before"
            },
            IsMenu = true,
            SortOrder = 998
        };

        var createResponse = await client.PostAsJsonAsync("/api/access/functions", create);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.ReadDataAsync<FunctionNodeDto>();
        Assert.NotNull(created);

        var update = new UpdateFunctionRequest
        {
            DisplayName = new()
            {
                ["zh"] = "更新后",
                ["ja"] = "更新後",
                ["en"] = "After"
            }
        };

        var response = await client.PutAsJsonAsync($"/api/access/functions/{created!.Id}?lang=ja", update);
        response.EnsureSuccessStatusCode();

        var root = await response.ReadDataAsJsonAsync();

        Assert.True(root.TryGetProperty("displayName", out var displayName));
        Assert.Equal("更新後", displayName.GetString());
        Assert.False(root.TryGetProperty("displayNameTranslations", out _));
    }

    [Fact]
    public async Task TreeStructure_LanguageConsistency()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/access/functions?lang=ja");
        response.EnsureSuccessStatusCode();

        var root = await response.ReadDataAsJsonAsync();
        AssertTreeLanguageMode(root, expectedSingleLanguage: true);
    }

    private static void AssertTreeLanguageMode(JsonElement root, bool expectedSingleLanguage)
    {
        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        foreach (var node in root.EnumerateArray())
        {
            AssertNodeLanguageMode(node, expectedSingleLanguage);
        }
    }

    private static void AssertNodeLanguageMode(JsonElement node, bool expectedSingleLanguage)
    {
        Assert.Equal(JsonValueKind.Object, node.ValueKind);

        var hasDisplayName = node.TryGetProperty("displayName", out _);
        var hasTranslations = node.TryGetProperty("displayNameTranslations", out _);

        if (expectedSingleLanguage)
        {
            Assert.True(hasDisplayName);
            Assert.False(hasTranslations);
        }
        else
        {
            Assert.False(hasDisplayName);
            Assert.False(hasTranslations && hasDisplayName);
        }

        if (node.TryGetProperty("children", out var children) && children.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in children.EnumerateArray())
            {
                AssertNodeLanguageMode(child, expectedSingleLanguage);
            }
        }
    }

    private static bool TryFindNodeByCode(JsonElement element, string code, out JsonElement node)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
            {
                if (TryFindNodeByCode(child, code, out node))
                {
                    return true;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("code", out var codeProp) &&
                string.Equals(codeProp.GetString(), code, StringComparison.OrdinalIgnoreCase))
            {
                node = element;
                return true;
            }

            if (element.TryGetProperty("children", out var children) && children.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in children.EnumerateArray())
                {
                    if (TryFindNodeByCode(child, code, out node))
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
