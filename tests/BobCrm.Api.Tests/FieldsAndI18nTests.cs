using System.Text.Json;
using System.Net.Http.Json;

namespace BobCrm.Api.Tests;

public class FieldsAndI18nTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    public FieldsAndI18nTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Fields_And_Tags_Ok()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var defs = await client.GetFromJsonAsync<JsonElement>("/api/fields");
        Assert.True(defs.GetArrayLength() >= 3);
        // verify actions exist for email/link/file/rds
        bool HasAction(string key, string action)
        {
            foreach (var def in defs.EnumerateArray())
            {
                if (def.GetProperty("key").GetString()?.Equals(key, StringComparison.OrdinalIgnoreCase) == true)
                {
                    var actions = def.GetProperty("actions");
                    return actions.EnumerateArray().Any(a => a.TryGetProperty("action", out var act) && string.Equals(act.GetString(), action, StringComparison.OrdinalIgnoreCase));
                }
            }
            return false;
        }
        Assert.True(HasAction("email", "mailto"));
        Assert.True(HasAction("link", "openLink"));
        Assert.True(HasAction("file", "copy"));
        Assert.True(HasAction("rds", "downloadRdp"));

        var tags = await client.GetFromJsonAsync<JsonElement>("/api/fields/tags");
        Assert.True(tags.GetArrayLength() > 0);
        Assert.True(tags[0].TryGetProperty("tag", out _));
    }

    [Fact]
    public async Task I18n_Ok()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var all = await client.GetFromJsonAsync<JsonElement>("/api/i18n/resources");
        Assert.True(all.GetArrayLength() > 0);

        var zh = await client.GetFromJsonAsync<JsonElement>("/api/i18n/zh");
        Assert.True(zh.EnumerateObject().Any());

        var en = await client.GetFromJsonAsync<JsonElement>("/api/i18n/en");
        Assert.True(en.EnumerateObject().Any());

        var ja = await client.GetFromJsonAsync<JsonElement>("/api/i18n/ja");
        Assert.True(ja.EnumerateObject().Any());
        // check a known key exists and is mapped
        Assert.True(ja.TryGetProperty("LBL_CUSTOMER", out var val) && !string.IsNullOrWhiteSpace(val.GetString()));
    }
}
