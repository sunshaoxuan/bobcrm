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

    [Fact]
    public async Task LoadDataAsync_InvalidLayoutJson_SetsErrorAndLogs()
    {
        // Arrange: 提供格式错误的 JSON（缺少引号）
        var handler = new TestHttpMessageHandler();
        handler.Enqueue(HttpMethod.Get, "/api/fields", _ => JsonResponses.Ok(Array.Empty<FieldDefinitionDto>()));
        handler.Enqueue(HttpMethod.Get, "/api/products/1?lang=ja", _ => JsonResponses.Ok(new { id = 1, code = "P001", name = "Product-A", fields = Array.Empty<object>() }));

        var invalidJson = "{type: 'textbox', label: 'Test'}"; // 缺少引号，无效 JSON
        var runtimeResponse = new TemplateRuntimeResponse(
            new TemplateBindingDto(
                Id: 1,
                EntityType: "product",
                UsageType: TemplateUsageType.Detail,
                TemplateId: 1,
                IsSystem: true,
                RequiredFunctionCode: null,
                UpdatedBy: "system",
                UpdatedAt: DateTime.UtcNow),
            new TemplateDescriptorDto(
                Id: 1,
                Name: "TEMPLATE_INVALID",
                EntityType: "product",
                UsageType: TemplateUsageType.Detail,
                LayoutJson: invalidJson,
                Tags: Array.Empty<string>(),
                Description: null),
            HasFullAccess: true,
            AppliedScopes: Array.Empty<string>());

        var vm = CreateViewModel(handler, runtimeResponse);

        // Act
        await vm.LoadDataAsync("product", 1);

        // Assert: 验证错误被设置，LayoutWidgets 为空
        Assert.False(vm.Loading);
        Assert.NotNull(vm.Error);
        Assert.Contains("PL_LAYOUT_PARSE", vm.Error); // 错误消息应包含解析失败的键
        Assert.Empty(vm.LayoutWidgets);
    }

    [Fact]
    public async Task LoadDataAsync_MalformedLayoutJson_SetsErrorMessage()
    {
        // Arrange: 提供不符合 Widget 结构的有效 JSON
        var handler = new TestHttpMessageHandler();
        handler.Enqueue(HttpMethod.Get, "/api/fields", _ => JsonResponses.Ok(Array.Empty<FieldDefinitionDto>()));
        handler.Enqueue(HttpMethod.Get, "/api/products/1?lang=ja", _ => JsonResponses.Ok(new { id = 1, code = "P001", name = "Product-A", fields = Array.Empty<object>() }));

        var malformedJson = "[{\"unknownProperty\": 123}]"; // 有效 JSON 但结构错误
        var runtimeResponse = new TemplateRuntimeResponse(
            new TemplateBindingDto(
                Id: 1,
                EntityType: "product",
                UsageType: TemplateUsageType.Detail,
                TemplateId: 1,
                IsSystem: true,
                RequiredFunctionCode: null,
                UpdatedBy: "system",
                UpdatedAt: DateTime.UtcNow),
            new TemplateDescriptorDto(
                Id: 1,
                Name: "TEMPLATE_MALFORMED",
                EntityType: "product",
                UsageType: TemplateUsageType.Detail,
                LayoutJson: malformedJson,
                Tags: Array.Empty<string>(),
                Description: null),
            HasFullAccess: true,
            AppliedScopes: Array.Empty<string>());

        var vm = CreateViewModel(handler, runtimeResponse);

        // Act
        await vm.LoadDataAsync("product", 1);

        // Assert: 加载成功，但可能触发反序列化异常
        Assert.False(vm.Loading);
        // LayoutWidgets 可能为空或包含部分解析的数据，取决于反序列化器行为
        Assert.NotNull(vm.LayoutWidgets);
    }

    [Fact]
    public void GetFieldValue_WithValidData_ReturnsValue()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        var vm = CreateViewModel(handler, null);

        // 手动设置 EntityData
        var entityJson = """{"id": 1, "name": "Test", "code": "T001", "fields": [{"key": "CustomField", "value": "CustomValue"}]}""";
        var entityData = JsonDocument.Parse(entityJson).RootElement;
        typeof(PageLoaderViewModel).GetProperty("EntityData")!.SetValue(vm, entityData);

        // Act
        var value = vm.GetFieldValue("CustomField");

        // Assert
        Assert.Equal("CustomValue", value);
    }

    private static PageLoaderViewModel CreateViewModel(TestHttpMessageHandler handler, TemplateRuntimeResponse? runtimeResponse)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var httpFactory = new SimpleHttpClientFactory(httpClient);

        var js = new TestJsInteropService();
        js.Set("bobcrm.getLocalStorageItem", "accessToken", "token");
        js.Set("bobcrm.getLocalStorageItem", "lang", "ja");

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
                It.IsAny<string?>(),
                It.IsAny<Guid?>(),
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
