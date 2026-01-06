using System.Net.Http;
using System.Text.Json;
using BobCrm.Api.Abstractions;
using BobCrm.App.Models;
using BobCrm.App.Services;
using BobCrm.App.Services.Runtime;
using BobCrm.App.Tests.TestHelpers;
using BobCrm.App.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace BobCrm.App.Tests;

public class PageLoaderViewModelTests
{
    [Fact]
    public async Task LoadDataAsync_AppendsLangQuery_ToEntityGet()
    {
        var handler = new TestHttpMessageHandler();
        handler.Enqueue(HttpMethod.Get, "/api/fields", _ => JsonResponses.Ok(Array.Empty<FieldDefinitionDto>()));
        handler.Enqueue(HttpMethod.Get, "/api/orders/1?lang=ja", _ => JsonResponses.Ok(new { id = 1, code = "O001", name = "Order-A", fields = Array.Empty<object>() }));

        var vm = CreateViewModel(handler, runtimeResponse: null);

        await vm.LoadDataAsync("order", 1);

        Assert.Contains(handler.CapturedRequests, r => r.Method == HttpMethod.Get && r.Path == "/api/orders/1?lang=ja");
        Assert.False(vm.Loading);
        Assert.NotNull(vm.EntityData);
    }

    [Fact]
    public async Task SaveChangesAsync_SendsPut_AndReloads()
    {
        var handler = new TestHttpMessageHandler();

        handler.Enqueue(HttpMethod.Get, "/api/fields", _ => JsonResponses.Ok(Array.Empty<FieldDefinitionDto>()));
        handler.Enqueue(HttpMethod.Get, "/api/orders/1?lang=ja", _ => JsonResponses.Ok(new { id = 1, code = "O001", name = "Order-A", fields = Array.Empty<object>() }));

        handler.Enqueue(HttpMethod.Put, "/api/orders/1", _ => JsonResponses.Ok(new { ok = true }));

        handler.Enqueue(HttpMethod.Get, "/api/fields", _ => JsonResponses.Ok(Array.Empty<FieldDefinitionDto>()));
        handler.Enqueue(HttpMethod.Get, "/api/orders/1?lang=ja", _ => JsonResponses.Ok(new { id = 1, code = "O002", name = "Order-B", fields = Array.Empty<object>() }));

        var vm = CreateViewModel(handler, runtimeResponse: new TemplateRuntimeResponse(
            new TemplateBindingDto(
                Id: 1,
                EntityType: "order",
                UsageType: TemplateUsageType.Detail,
                TemplateId: 1,
                IsSystem: true,
                RequiredFunctionCode: null,
                UpdatedBy: "system",
                UpdatedAt: DateTime.UtcNow),
            new TemplateDescriptorDto(
                Id: 1,
                Name: "TEMPLATE_TEST",
                EntityType: "order",
                UsageType: TemplateUsageType.Detail,
                LayoutJson: """[{ "type": "textbox", "dataField": "customField", "label": "Custom", "visible": true }]""",
                Tags: Array.Empty<string>(),
                Description: null),
            HasFullAccess: true,
            AppliedScopes: Array.Empty<string>()));

        await vm.LoadDataAsync("order", 1);
        await vm.EnterEditModeAsync();

        vm.EditCode = "O002";
        vm.EditName = "Order-B";

        await vm.SaveChangesAsync();

        Assert.Contains(handler.CapturedRequests, r => r.Method == HttpMethod.Put && r.Path == "/api/orders/1");
        Assert.Contains(handler.CapturedRequests, r => r.Method == HttpMethod.Get && r.Path == "/api/orders/1?lang=ja");
        Assert.False(vm.IsEditMode);
    }

    private static PageLoaderViewModel CreateViewModel(TestHttpMessageHandler handler, TemplateRuntimeResponse? runtimeResponse)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var httpFactory = new SimpleHttpClientFactory(httpClient);

        var js = new TestJsInteropService();
        js.Set("localStorage.getItem", "accessToken", "token");
        js.Set("bobcrm.getCookie", "lang", "ja");

        var auth = new AuthService(httpFactory, js, TimeProvider.System);
        var i18n = new FakeI18nService("ja");
        var fieldService = new FieldService(httpFactory, auth);
        var labelService = new RuntimeLabelService(i18n, fieldService);
        var legacy = new LegacyLayoutParser();

        var mockRuntime = new Mock<TemplateRuntimeClient>(MockBehavior.Strict, auth, NullLogger<TemplateRuntimeClient>.Instance);
        mockRuntime.Setup(x => x.GetRuntimeAsync(
                It.IsAny<string>(),
                It.IsAny<TemplateUsageType>(),
                It.IsAny<string?>(),
                It.IsAny<int?>(),
                It.IsAny<JsonElement?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(runtimeResponse);

        return new PageLoaderViewModel(
            auth,
            i18n,
            js,
            mockRuntime.Object,
            legacy,
            labelService,
            NullLogger<PageLoaderViewModel>.Instance);
    }

    private sealed class FakeI18nService : II18nService
    {
        public FakeI18nService(string lang) => CurrentLang = lang;

        public string CurrentLang { get; private set; }
        public event Action? OnChanged;

        public string T(string key) => key;

        public Task LoadAsync(string lang, bool force = false, CancellationToken ct = default)
        {
            CurrentLang = lang;
            OnChanged?.Invoke();
            return Task.CompletedTask;
        }
    }
}

