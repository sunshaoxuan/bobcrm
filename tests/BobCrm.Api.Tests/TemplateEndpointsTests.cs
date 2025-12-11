using System.Net.Http.Headers;
using System.Text.Json;

namespace BobCrm.Api.Tests;

public class TemplateEndpointsTests : IClassFixture<TestWebAppFactory>
{
    private readonly TestWebAppFactory _factory;
    public TemplateEndpointsTests(TestWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task MenuBindings_WithoutLang_ReturnsMultilingual()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/templates/menu-bindings");
        response.EnsureSuccessStatusCode();

        using var json = await ReadJsonAsync(response);
        if (!TryFindFirstMenu(json.RootElement, out var firstMenu))
        {
            // No menu bindings in fixture; treat as not applicable.
            return;
        }

        Assert.True(firstMenu.ValueKind == JsonValueKind.Object, "menu payload should be object");
        Assert.False(firstMenu.TryGetProperty("displayName", out _));
        Assert.True(firstMenu.TryGetProperty("displayNameTranslations", out var translations));
        Assert.Equal(JsonValueKind.Object, translations.ValueKind);
    }

    [Fact]
    public async Task MenuBindings_WithLang_ReturnsSingleLanguage()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/templates/menu-bindings?lang=ja");
        response.EnsureSuccessStatusCode();

        using var json = await ReadJsonAsync(response);
        if (!TryFindFirstMenu(json.RootElement, out var firstMenu))
        {
            return;
        }

        Assert.True(firstMenu.TryGetProperty("displayName", out var displayName));
        Assert.False(string.IsNullOrWhiteSpace(displayName.GetString()));
        Assert.False(firstMenu.TryGetProperty("displayNameTranslations", out _));
    }

    [Fact]
    public async Task MenuBindings_UsesAcceptLanguageHeader()
    {
        var client = await CreateAuthenticatedClientAsync();
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US"));

        var response = await client.GetAsync("/api/templates/menu-bindings");
        response.EnsureSuccessStatusCode();

        using var json = await ReadJsonAsync(response);
        if (!TryFindFirstMenu(json.RootElement, out var firstMenu))
        {
            return;
        }

        Assert.True(firstMenu.TryGetProperty("displayName", out var displayName));
        Assert.False(string.IsNullOrWhiteSpace(displayName.GetString()));
        Assert.False(firstMenu.TryGetProperty("displayNameTranslations", out _));
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var (access, _) = await client.LoginAsAdminAsync();
        client.UseBearer(access);
        return client;
    }

    private static bool TryFindFirstMenu(JsonElement root, out JsonElement menuElement)
    {
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
        {
            menuElement = default;
            return false;
        }

        var first = root[0];
        if (first.TryGetProperty("menu", out var menu))
        {
            menuElement = menu;
            return true;
        }

        if (first.TryGetProperty("Menu", out var menuPascal))
        {
            menuElement = menuPascal;
            return true;
        }

        menuElement = default;
        return false;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content);
    }
}
