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

        var defsResp = await client.GetAsync("/api/fields");
        defsResp.EnsureSuccessStatusCode();
        var defs = await defsResp.ReadDataAsJsonAsync();
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

        var tagsResp = await client.GetAsync("/api/fields/tags");
        tagsResp.EnsureSuccessStatusCode();
        var tags = await tagsResp.ReadDataAsJsonAsync();
        Assert.True(tags.GetArrayLength() > 0);
        Assert.True(tags[0].TryGetProperty("tag", out _));
    }

    [Fact]
    public async Task I18n_Ok()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var allResp = await client.GetAsync("/api/i18n/resources");
        allResp.EnsureSuccessStatusCode();
        var all = await allResp.ReadDataAsJsonAsync();
        Assert.True(all.GetArrayLength() > 0);

        var zhResp = await client.GetAsync("/api/i18n/zh");
        zhResp.EnsureSuccessStatusCode();
        var zh = await zhResp.ReadDataAsJsonAsync();
        Assert.True(zh.EnumerateObject().Any());

        var enResp = await client.GetAsync("/api/i18n/en");
        enResp.EnsureSuccessStatusCode();
        var en = await enResp.ReadDataAsJsonAsync();
        Assert.True(en.EnumerateObject().Any());

        var jaResp = await client.GetAsync("/api/i18n/ja");
        jaResp.EnsureSuccessStatusCode();
        var ja = await jaResp.ReadDataAsJsonAsync();
        Assert.True(ja.EnumerateObject().Any());
        // check a known key exists and is mapped
        Assert.True(ja.TryGetProperty("LBL_CUSTOMER", out var val) && !string.IsNullOrWhiteSpace(val.GetString()));
    }
}
