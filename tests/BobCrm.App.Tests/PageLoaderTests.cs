using System.Net;
using System.Net.Http;
using System.Text.Json;
using AntDesign;
using BobCrm.App.Components.Pages;
using BobCrm.App.Models;
using BobCrm.App.Services;
using BobCrm.App.Services.Widgets.Rendering;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Moq;

namespace BobCrm.App.Tests;

public class PageLoaderTests : TestContext
{
    private readonly TestHttpMessageHandler _handler = new();
    private readonly TemplateRuntimeResponse _runtimeResponse;

    public PageLoaderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.Setup<string?>("localStorage.getItem", "accessToken").SetResult("token");
        JSInterop.Setup<string?>("bobcrm.getCookie", "lang").SetResult("ja");

        var httpClient = new HttpClient(_handler) { BaseAddress = new Uri("http://localhost") };
        var httpFactory = new SimpleHttpClientFactory(httpClient);

        Services.AddSingleton<IHttpClientFactory>(httpFactory);
        Services.AddSingleton(TimeProvider.System);
        Services.AddLogging();
        Services.AddAntDesign();
        Services.AddScoped<NavigationManager, FakeNavigationManager>();
        Services.AddScoped<ToastService>();
        Services.AddScoped<IJsInteropService, JsInteropService>();
        Services.AddScoped(sp => new AuthService(httpFactory, sp.GetRequiredService<IJsInteropService>(), sp.GetRequiredService<TimeProvider>()));
        Services.AddScoped(sp => new I18nService(httpFactory, sp.GetRequiredService<AuthService>(), sp.GetRequiredService<IJsInteropService>()));
        Services.AddScoped(sp => new FieldService(httpFactory, sp.GetRequiredService<AuthService>()));
        Services.AddScoped<FieldActionService>();

        _runtimeResponse = new TemplateRuntimeResponse(
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
            AppliedScopes: Array.Empty<string>());

        Services.AddScoped(sp =>
        {
            var auth = sp.GetRequiredService<AuthService>();
            var mock = new Mock<TemplateRuntimeClient>(MockBehavior.Strict, auth, NullLogger<TemplateRuntimeClient>.Instance);
            mock.Setup(x => x.GetRuntimeAsync(
                    It.IsAny<string>(),
                    It.IsAny<TemplateUsageType>(),
                    It.IsAny<string?>(),
                    It.IsAny<int?>(),
                    It.IsAny<JsonElement?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(_runtimeResponse);
            return mock.Object;
        });

        Services.AddScoped<IRuntimeWidgetRenderer>(_ =>
        {
            var mock = new Mock<IRuntimeWidgetRenderer>(MockBehavior.Strict);
            mock.Setup(r => r.Render(It.IsAny<RuntimeWidgetRenderRequest>()))
                .Returns((RuntimeWidgetRenderRequest req) => builder =>
                {
                    builder.OpenElement(0, "div");
                    builder.AddAttribute(1, "data-testid", "runtime-widget-rendered");
                    builder.AddContent(2, req.Widget.Type);
                    builder.CloseElement();
                });
            return mock.Object;
        });
    }

    [Fact]
    public void LoadData_AppendsLangQuery_ToEntityGet()
    {
        EnqueueCommonResponses(entityName: "Order-A", entityCode: "O001");

        var cut = RenderComponent<PageLoader>(p => p
            .Add(x => x.EntityType, "order")
            .Add(x => x.Id, 1));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(_handler.CapturedRequests, r => r.Method == HttpMethod.Get && r.Path == "/api/orders/1?lang=ja");
        });
    }

    [Fact]
    public void Renders_RuntimeWidgets_FromTemplateLayout()
    {
        EnqueueCommonResponses(entityName: "Order-A", entityCode: "O001");

        var cut = RenderComponent<PageLoader>(p => p
            .Add(x => x.EntityType, "order")
            .Add(x => x.Id, 1));

        cut.WaitForAssertion(() =>
        {
            var rendered = cut.Find("[data-testid='runtime-widget-rendered']");
            Assert.Contains("textbox", rendered.TextContent);
        });
    }

    [Fact]
    public void EditAndSave_SendsPut_AndReloadsWithLangQuery()
    {
        EnqueueCommonResponses(entityName: "Order-A", entityCode: "O001");

        _handler.Enqueue(HttpMethod.Put, "/api/orders/1", _ => JsonResponse(new { ok = true }));

        // Reload after save
        _handler.Enqueue(HttpMethod.Get, "/api/fields", _ => JsonResponse(Array.Empty<FieldDefinitionDto>()));
        _handler.Enqueue(HttpMethod.Get, "/api/orders/1?lang=ja", _ => JsonResponse(new
        {
            id = 1,
            code = "O002",
            name = "Order-B",
            fields = Array.Empty<object>()
        }));

        var cut = RenderComponent<PageLoader>(p => p
            .Add(x => x.EntityType, "order")
            .Add(x => x.Id, 1));

        cut.WaitForAssertion(() => cut.FindAll("button").Any(b => b.TextContent.Contains("MODE_EDIT")));
        cut.FindAll("button").First(b => b.TextContent.Contains("MODE_EDIT")).Click();

        cut.WaitForAssertion(() => Assert.True(cut.FindAll(".runtime-basic-grid input.runtime-field-input").Count >= 2));
        var inputs = cut.FindAll(".runtime-basic-grid input.runtime-field-input");
        inputs[0].Input("O002");
        inputs[1].Input("Order-B");

        cut.FindAll("button").First(b => b.TextContent.Contains("BTN_SAVE")).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(_handler.CapturedRequests, r => r.Method == HttpMethod.Put && r.Path == "/api/orders/1");
            Assert.Contains(_handler.CapturedRequests, r => r.Method == HttpMethod.Get && r.Path == "/api/orders/1?lang=ja");
        });
    }

    private void EnqueueCommonResponses(string entityName, string entityCode)
    {
        _handler.Enqueue(HttpMethod.Get, "/api/fields", _ => JsonResponse(Array.Empty<FieldDefinitionDto>()));
        _handler.Enqueue(HttpMethod.Get, "/api/orders/1?lang=ja", _ => JsonResponse(new
        {
            id = 1,
            code = entityCode,
            name = entityName,
            fields = Array.Empty<object>()
        }));
    }

    private static HttpResponseMessage JsonResponse(object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

    private sealed class SimpleHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public SimpleHttpClientFactory(HttpClient client) => _client = client;

        public HttpClient CreateClient(string name) => _client;
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<(HttpMethod Method, string Path), Queue<Func<HttpRequestMessage, Task<HttpResponseMessage>>>> _routes = new();
        public List<CapturedRequest> CapturedRequests { get; } = new();

        public void Enqueue(HttpMethod method, string path, Func<HttpRequestMessage, HttpResponseMessage> responder) =>
            Enqueue(method, path, request => Task.FromResult(responder(request)));

        public void Enqueue(HttpMethod method, string path, Func<HttpRequestMessage, Task<HttpResponseMessage>> responder)
        {
            var key = (method, path);
            if (!_routes.TryGetValue(key, out var queue))
            {
                queue = new Queue<Func<HttpRequestMessage, Task<HttpResponseMessage>>>();
                _routes[key] = queue;
            }

            queue.Enqueue(responder);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.PathAndQuery ?? string.Empty;
            var key = (request.Method, path);
            if (!_routes.TryGetValue(key, out var queue) || queue.Count == 0)
            {
                throw new InvalidOperationException($"No handler registered for {request.Method} {path}");
            }

            var body = request.Content != null ? await request.Content.ReadAsStringAsync(cancellationToken) : null;
            CapturedRequests.Add(new CapturedRequest(request.Method, path, body));

            var responder = queue.Dequeue();
            var response = await responder(request);
            response.RequestMessage = request;
            return response;
        }
    }

    public record CapturedRequest(HttpMethod Method, string Path, string? Body);
}
