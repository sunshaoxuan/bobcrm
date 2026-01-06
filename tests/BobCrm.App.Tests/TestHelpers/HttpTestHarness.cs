using System.Net.Http;
using System.Text.Json;
using BobCrm.App.Services;

namespace BobCrm.App.Tests.TestHelpers;

public sealed class SimpleHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient _client;

    public SimpleHttpClientFactory(HttpClient client) => _client = client;

    public HttpClient CreateClient(string name) => _client;
}

public sealed class TestHttpMessageHandler : HttpMessageHandler
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

    public record CapturedRequest(HttpMethod Method, string Path, string? Body);
}

public sealed class TestJsInteropService : IJsInteropService
{
    private readonly Dictionary<(string Identifier, string? Arg0), object?> _values = new();

    public void Set(string identifier, string? arg0, object? value) => _values[(identifier, arg0)] = value;

    public Task<(bool Success, T? Value)> TryInvokeAsync<T>(string identifier, params object?[]? args)
    {
        var arg0 = args is { Length: > 0 } ? args[0]?.ToString() : null;
        if (_values.TryGetValue((identifier, arg0), out var raw))
        {
            return Task.FromResult((true, (T?)raw));
        }

        return Task.FromResult<(bool, T?)>((true, default));
    }

    public Task<bool> TryInvokeVoidAsync(string identifier, params object?[]? args) => Task.FromResult(true);

    public Task<bool> TryInvokeVoidWithToastAsync(string identifier, Func<string> errorMessageFactory, params object?[]? args) =>
        Task.FromResult(true);
}

public static class JsonResponses
{
    public static HttpResponseMessage Ok(object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }
}

