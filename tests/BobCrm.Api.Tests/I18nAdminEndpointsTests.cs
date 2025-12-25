using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BobCrm.Api.Contracts.Requests.I18n;

namespace BobCrm.Api.Tests;

public class I18nAdminEndpointsTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;

    public I18nAdminEndpointsTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task I18n_Admin_Search_Save_And_Reload_Works()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var searchResp = await client.GetAsync("/api/system/i18n?page=1&pageSize=5&key=LBL_CUSTOMER&culture=ja");
        searchResp.EnsureSuccessStatusCode();
        var searchData = await searchResp.ReadDataAsJsonAsync();
        Assert.True(searchData.ValueKind == JsonValueKind.Array);

        var saveResp = await client.PostAsJsonAsync("/api/system/i18n", new SaveI18nResourceRequest
        {
            Key = "TEST_I18N_KEY",
            Culture = "en",
            Value = "Hello",
            Force = true
        });
        saveResp.EnsureSuccessStatusCode();

        var dictResp = await client.GetAsync("/api/i18n/en");
        dictResp.EnsureSuccessStatusCode();
        var dict = await dictResp.ReadDataAsJsonAsync();
        Assert.True(dict.TryGetProperty("TEST_I18N_KEY", out var v) && v.GetString() == "Hello");

        var reloadResp = await client.PostAsync("/api/system/i18n/reload", content: null);
        reloadResp.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task I18n_Admin_Save_Protected_Key_Requires_Force()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);

        var resp = await client.PostAsJsonAsync("/api/system/i18n", new SaveI18nResourceRequest
        {
            Key = "BTN_SAVE",
            Culture = "en",
            Value = "Save (Test)",
            Force = false
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = JsonDocument.Parse(await resp.Content.ReadAsStringAsync()).RootElement;
        Assert.True(body.TryGetProperty("code", out var code));
        Assert.Equal("I18N_KEY_PROTECTED", code.GetString());

        var forced = await client.PostAsJsonAsync("/api/system/i18n", new SaveI18nResourceRequest
        {
            Key = "BTN_SAVE",
            Culture = "en",
            Value = "Save (Test)",
            Force = true
        });
        forced.EnsureSuccessStatusCode();
    }
}
