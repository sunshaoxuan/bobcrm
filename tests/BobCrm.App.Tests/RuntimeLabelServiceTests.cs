using System.Net.Http;
using BobCrm.Api.Abstractions;
using BobCrm.App.Models;
using BobCrm.App.Services;
using BobCrm.App.Services.Runtime;
using BobCrm.App.Services.Widgets;
using BobCrm.App.Tests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;

namespace BobCrm.App.Tests;

public class RuntimeLabelServiceTests
{
    [Fact]
    public async Task ResolveLabel_PrefersWidgetLabelTranslation_WhenLabelLooksLikeKey()
    {
        var i18n = new FakeI18nService(("COL_CODE", "代码"));
        var service = await CreateServiceAsync(i18n, Array.Empty<FieldDefinitionDto>());

        var widget = WidgetRegistry.Create("textbox", "COL_CODE");
        widget.DataField = "code";

        var label = service.ResolveLabel(widget, entityData: null);
        Assert.Equal("代码", label);
    }

    [Fact]
    public async Task ResolveLabel_UsesEnglishLabelMap_WhenLabelIsKnownEnglish()
    {
        var i18n = new FakeI18nService(("COL_NAME", "名称"));
        var service = await CreateServiceAsync(i18n, Array.Empty<FieldDefinitionDto>());

        var widget = WidgetRegistry.Create("textbox", "Customer Name");
        widget.DataField = "name";

        var label = service.ResolveLabel(widget, entityData: null);
        Assert.Equal("名称", label);
    }

    [Fact]
    public async Task ResolveLabel_PrefersEntityDataFieldLabel()
    {
        var i18n = new FakeI18nService(("COL_NAME", "名称"));
        var service = await CreateServiceAsync(i18n, Array.Empty<FieldDefinitionDto>());

        var widget = WidgetRegistry.Create("textbox", "customField");
        widget.DataField = "customField";

        var entityDataJson = """
                             {
                               "fields": [
                                 { "key": "customField", "label": "运行态标签", "value": "x" }
                               ]
                             }
                             """;
        using var doc = System.Text.Json.JsonDocument.Parse(entityDataJson);

        var label = service.ResolveLabel(widget, doc.RootElement);
        Assert.Equal("运行态标签", label);
    }

    [Fact]
    public async Task ResolveLabel_UsesFieldDefinitionLabel_WhenNoRuntimeLabel()
    {
        var i18n = new FakeI18nService(("LBL_CUSTOM_FIELD", "自定义字段"));
        var service = await CreateServiceAsync(i18n, new[]
        {
            new FieldDefinitionDto { key = "customField", label = "LBL_CUSTOM_FIELD" }
        });

        var widget = WidgetRegistry.Create("textbox", "customField");
        widget.DataField = "customField";

        var label = service.ResolveLabel(widget, entityData: null);
        Assert.Equal("自定义字段", label);
    }

    [Fact]
    public async Task ResolveLabel_FallsBackToBaseResourceMap()
    {
        var i18n = new FakeI18nService(("COL_CODE", "代码"));
        var service = await CreateServiceAsync(i18n, Array.Empty<FieldDefinitionDto>());

        var widget = WidgetRegistry.Create("textbox", string.Empty);
        widget.DataField = "code";

        var label = service.ResolveLabel(widget, entityData: null);
        Assert.Equal("代码", label);
    }

    private static async Task<RuntimeLabelService> CreateServiceAsync(FakeI18nService i18n, IReadOnlyList<FieldDefinitionDto> defs)
    {
        var handler = new TestHttpMessageHandler();
        handler.Enqueue(HttpMethod.Get, "/api/fields", _ => JsonResponses.Ok(defs));

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var httpFactory = new SimpleHttpClientFactory(httpClient);

        var js = new TestJsInteropService();
        js.Set("localStorage.getItem", "accessToken", "token");
        js.Set("bobcrm.getCookie", "lang", "ja");

        var auth = new AuthService(httpFactory, js, TimeProvider.System);
        var fieldService = new FieldService(httpFactory, auth);
        var service = new RuntimeLabelService(i18n, fieldService);
        await service.RefreshFieldLabelsAsync();
        return service;
    }

    private sealed class FakeI18nService : II18nService
    {
        private readonly Dictionary<string, string> _dict = new(StringComparer.OrdinalIgnoreCase);

        public FakeI18nService(params (string Key, string Value)[] entries)
        {
            foreach (var (key, value) in entries)
            {
                _dict[key] = value;
            }
        }

        public string CurrentLang { get; private set; } = "ja";
        public event Action? OnChanged;

        public string T(string key) => _dict.TryGetValue(key, out var v) ? v : key;

        public Task LoadAsync(string lang, bool force = false, CancellationToken ct = default)
        {
            CurrentLang = lang;
            OnChanged?.Invoke();
            return Task.CompletedTask;
        }
    }
}
