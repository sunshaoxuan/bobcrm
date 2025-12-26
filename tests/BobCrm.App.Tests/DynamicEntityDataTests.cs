using System.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AntDesign;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;
using BobCrm.App.Components.Pages;
using BobCrm.App.Models;
using BobCrm.App.Services;

namespace BobCrm.App.Tests;

public class DynamicEntityDataTests : TestContext
{
    private readonly TestHttpMessageHandler _handler = new();
    private readonly Guid _definitionId = Guid.NewGuid();
    private const string FullTypeName = "Test.Namespace.SampleEntity";

    public DynamicEntityDataTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        var httpClientFactory = new FakeHttpClientFactory(httpClient);

        Services.AddSingleton<IHttpClientFactory>(httpClientFactory);
        Services.AddAntDesign();
        Services.AddSingleton(TimeProvider.System);
        Services.AddScoped<IJsInteropService, JsInteropService>();
        Services.AddScoped(sp => new AuthService(httpClientFactory, sp.GetRequiredService<IJsInteropService>(), sp.GetRequiredService<TimeProvider>()));
        Services.AddScoped(sp => new EntityDefinitionService(sp.GetRequiredService<AuthService>()));
        Services.AddScoped(sp => new DynamicEntityService(sp.GetRequiredService<AuthService>()));
        Services.AddScoped(sp => new I18nService(httpClientFactory, sp.GetRequiredService<AuthService>(), sp.GetRequiredService<IJsInteropService>()));
        Services.AddScoped<ToastService>();
        Services.AddScoped<NavigationManager, FakeNavigationManager>();
    }

    [Fact]
    public void CreateEntity_SubmitsPayloadWithAuditFields()
    {
        SetupMetadata();
        _handler.Enqueue(HttpMethod.Post, $"/api/dynamic-entities/{FullTypeName}/query", _ => JsonResponse(new
        {
            Data = Array.Empty<Dictionary<string, object>>(),
            Total = 0,
            Page = 1,
            PageSize = 100
        }));
        _handler.Enqueue(HttpMethod.Post, $"/api/dynamic-entities/{FullTypeName}", async request =>
        {
            var payload = await request.ReadJsonAsync();
            Assert.Equal("P001", payload.GetProperty("Code").GetString());
            Assert.True(payload.TryGetProperty("CreatedAt", out _));
            Assert.True(payload.TryGetProperty("CreatedBy", out _));
            return JsonResponse(new
            {
                Id = 1,
                Code = "P001",
                Name = "Sample",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        });
        _handler.Enqueue(HttpMethod.Post, $"/api/dynamic-entities/{FullTypeName}/query", _ => JsonResponse(new
        {
            Data = new[]
            {
                new Dictionary<string, object>
                {
                    ["Id"] = 1,
                    ["Code"] = "P001",
                    ["Name"] = "Sample"
                }
            },
            Total = 1,
            Page = 1,
            PageSize = 100
        }));

        var cut = RenderComponent<DynamicEntityData>(parameters => parameters.Add(p => p.FullTypeName, FullTypeName));

        cut.WaitForAssertion(() => Assert.Contains("BTN_CREATE", cut.Markup));

        cut.FindAll("button").First(b => b.TextContent.Contains("BTN_CREATE")).Click();

        var codeInput = cut.Find("#field-Code");
        codeInput.Change("P001");
        var nameInput = cut.Find("#field-Name");
        nameInput.Change("Sample");

        cut.Find(".ant-modal .ant-btn-primary").Click();

        cut.WaitForAssertion(() => Assert.Contains("P001", cut.Markup));

        Assert.Contains(_handler.CapturedRequests, r => r.Method == HttpMethod.Post && r.Path == $"/api/dynamic-entities/{FullTypeName}");
    }

    [Fact]
    public void EditEntity_SendsUpdateWithUpdatedAt()
    {
        SetupMetadata();
        _handler.Enqueue(HttpMethod.Post, $"/api/dynamic-entities/{FullTypeName}/query", _ => JsonResponse(new
        {
            Data = new[]
            {
                new Dictionary<string, object>
                {
                    ["Id"] = 1,
                    ["Code"] = "P001",
                    ["Name"] = "Sample",
                    ["UpdatedAt"] = DateTime.UtcNow
                }
            },
            Total = 1,
            Page = 1,
            PageSize = 100
        }));
        _handler.Enqueue(HttpMethod.Get, $"/api/dynamic-entities/{FullTypeName}/1", _ => JsonResponse(new
        {
            Id = 1,
            Code = "P001",
            Name = "Sample",
            UpdatedAt = DateTime.UtcNow
        }));
        _handler.Enqueue(HttpMethod.Put, $"/api/dynamic-entities/{FullTypeName}/1", async request =>
        {
            var payload = await request.ReadJsonAsync();
            Assert.True(payload.TryGetProperty("UpdatedAt", out _));
            return JsonResponse(new
            {
                Id = 1,
                Code = "P001-Updated",
                Name = "Sample",
                UpdatedAt = DateTime.UtcNow
            });
        });
        _handler.Enqueue(HttpMethod.Post, $"/api/dynamic-entities/{FullTypeName}/query", _ => JsonResponse(new
        {
            Data = new[]
            {
                new Dictionary<string, object>
                {
                    ["Id"] = 1,
                    ["Code"] = "P001-Updated",
                    ["Name"] = "Sample"
                }
            },
            Total = 1,
            Page = 1,
            PageSize = 100
        }));

        var cut = RenderComponent<DynamicEntityData>(parameters => parameters.Add(p => p.FullTypeName, FullTypeName));

        cut.WaitForAssertion(() => Assert.Contains("BTN_EDIT", cut.Markup));

        cut.FindAll("button").First(b => b.TextContent.Contains("BTN_EDIT")).Click();

        var codeInput = cut.Find("#field-Code");
        codeInput.Change("P001-Updated");

        cut.Find(".ant-modal .ant-btn-primary").Click();

        cut.WaitForAssertion(() => Assert.Contains("P001-Updated", cut.Markup));

        Assert.Contains(_handler.CapturedRequests, r => r.Method == HttpMethod.Put && r.Path == $"/api/dynamic-entities/{FullTypeName}/1");
    }

    [Fact]
    public void CreateEntity_ValidatesRequiredFields()
    {
        SetupMetadata();
        _handler.Enqueue(HttpMethod.Post, $"/api/dynamic-entities/{FullTypeName}/query", _ => JsonResponse(new
        {
            Data = Array.Empty<Dictionary<string, object>>(),
            Total = 0,
            Page = 1,
            PageSize = 100
        }));

        var cut = RenderComponent<DynamicEntityData>(parameters => parameters.Add(p => p.FullTypeName, FullTypeName));

        cut.WaitForAssertion(() => Assert.Contains("BTN_CREATE", cut.Markup));

        cut.FindAll("button").First(b => b.TextContent.Contains("BTN_CREATE")).Click();

        cut.Find(".ant-modal .ant-btn-primary").Click();

        cut.WaitForAssertion(() => Assert.Contains("form-item-error", cut.Markup));

        Assert.DoesNotContain(_handler.CapturedRequests, r => r.Path == $"/api/dynamic-entities/{FullTypeName}");
    }

    private void SetupMetadata()
    {
        _handler.Enqueue(HttpMethod.Get, "/api/entity-definitions", _ => JsonResponse(new[]
        {
            new
            {
                Id = _definitionId,
                FullTypeName,
                EntityName = "SampleEntity"
            }
        }));

        _handler.Enqueue(HttpMethod.Get, $"/api/entity-definitions/{_definitionId}", _ => JsonResponse(new
        {
            Id = _definitionId,
            Namespace = "Test.Namespace",
            EntityName = "SampleEntity",
            FullTypeName,
            Fields = new[]
            {
                new { PropertyName = "Id", DataType = FieldDataType.Integer, IsRequired = true, SortOrder = 1, Source = (string?)"System", DisplayName = (object?)null },
                new { PropertyName = "Code", DataType = FieldDataType.String, IsRequired = true, SortOrder = 2, Source = (string?)null, DisplayName = (object?)new { en = "Code" } },
                new { PropertyName = "Name", DataType = FieldDataType.String, IsRequired = false, SortOrder = 3, Source = (string?)null, DisplayName = (object?)new { en = "Name" } },
                new { PropertyName = "CreatedAt", DataType = FieldDataType.DateTime, IsRequired = true, SortOrder = 4, Source = (string?)"Interface", DisplayName = (object?)null },
                new { PropertyName = "CreatedBy", DataType = FieldDataType.String, IsRequired = false, SortOrder = 5, Source = (string?)"Interface", DisplayName = (object?)null },
                new { PropertyName = "UpdatedAt", DataType = FieldDataType.DateTime, IsRequired = true, SortOrder = 6, Source = (string?)"Interface", DisplayName = (object?)null },
                new { PropertyName = "UpdatedBy", DataType = FieldDataType.String, IsRequired = false, SortOrder = 7, Source = (string?)"Interface", DisplayName = (object?)null },
                new { PropertyName = "Version", DataType = FieldDataType.Integer, IsRequired = true, SortOrder = 8, Source = (string?)"Interface", DisplayName = (object?)null }
            }
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

    private class FakeHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public FakeHttpClientFactory(HttpClient client) => _client = client;

        public HttpClient CreateClient(string name) => _client;
    }

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<(HttpMethod Method, string Path), Queue<Func<HttpRequestMessage, Task<HttpResponseMessage>>>> _routes = new();
        public List<CapturedRequest> CapturedRequests { get; } = new();

        public void Enqueue(HttpMethod method, string path, Func<HttpRequestMessage, HttpResponseMessage> responder)
            => Enqueue(method, path, request => Task.FromResult(responder(request)));

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

internal static class HttpRequestMessageExtensions
{
    public static async Task<JsonElement> ReadJsonAsync(this HttpRequestMessage request)
    {
        var body = request.Content != null
            ? await request.Content.ReadAsStringAsync()
            : string.Empty;
        return JsonDocument.Parse(body).RootElement;
    }
}
