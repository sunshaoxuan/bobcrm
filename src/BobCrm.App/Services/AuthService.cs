using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace BobCrm.App.Services;

public class AuthService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IJSRuntime _js;

    public AuthService(IHttpClientFactory httpFactory, IJSRuntime js)
    {
        _httpFactory = httpFactory;
        _js = js;
    }

    private async Task<HttpClient> CreateBaseClientAsync()
    {
        var http = _httpFactory.CreateClient("api");
        // Prefer localStorage apiBase (来自 Setup 保存)，其次 cookie，再次同源
        try
        {
            var lsBase = await _js.InvokeAsync<string?>("localStorage.getItem", "apiBase");
            if (!string.IsNullOrWhiteSpace(lsBase))
            {
                http.BaseAddress = new Uri(NormalizeBase(lsBase!) , UriKind.Absolute);
            }
            else
            {
                var cookieBase = await _js.InvokeAsync<string?>("bobcrm.getCookie", "apiBase");
                if (!string.IsNullOrWhiteSpace(cookieBase))
                {
                    var normalized = NormalizeBase(cookieBase!);
                    http.BaseAddress = new Uri(normalized, UriKind.Absolute);
                    // 同步回写，避免再次回退
                    try { await _js.InvokeVoidAsync("localStorage.setItem", "apiBase", normalized); } catch { }
                    try { await _js.InvokeVoidAsync("bobcrm.setCookie", "apiBase", normalized, 365); } catch { }
                }
                else
                {
                    var origin = await _js.InvokeAsync<string?>("bobcrm.getOrigin");
                    if (!string.IsNullOrWhiteSpace(origin))
                        http.BaseAddress = new Uri(origin!, UriKind.Absolute);
                }
            }
        }
        catch { }
        // attach language header (no auth required)
        try
        {
            var lang = await _js.InvokeAsync<string?>("bobcrm.getCookie", "lang") ?? "ja";
            if (http.DefaultRequestHeaders.Contains("X-Lang"))
                http.DefaultRequestHeaders.Remove("X-Lang");
            http.DefaultRequestHeaders.Add("X-Lang", lang.ToLowerInvariant());
        }
        catch { }
        return http;
    }

    private static string NormalizeBase(string baseUrl)
    {
        try
        {
            var u = new Uri(baseUrl, UriKind.Absolute);
            // 策略：如果端口是 8081，自动切到 5200（与你的部署一致），避免无意回退。
            if (u.IsDefaultPort || u.Port != 8081) return baseUrl;
            var builder = new UriBuilder(u) { Port = 5200 };
            return builder.Uri.ToString().TrimEnd('/');
        }
        catch { return baseUrl; }
    }

    public async Task<HttpClient> CreateClientWithAuthAsync()
    {
        var http = await CreateBaseClientAsync();
        var access = await _js.InvokeAsync<string?>("localStorage.getItem", "accessToken");
        if (!string.IsNullOrWhiteSpace(access))
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
        }
        return http;
    }

    public Task<HttpClient> CreateClientWithLangAsync() => CreateBaseClientAsync();

    public async Task<HttpResponseMessage> GetWithRefreshAsync(string url)
    {
        var http = await CreateClientWithAuthAsync();
        var resp = await http.GetAsync(url);
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var refreshed = await TryRefreshAsync();
            if (refreshed)
            {
                http = await CreateClientWithAuthAsync();
                resp = await http.GetAsync(url);
            }
        }
        return resp;
    }

    public async Task<HttpResponseMessage> PostAsJsonWithRefreshAsync<T>(string url, T data)
    {
        var http = await CreateClientWithAuthAsync();
        var resp = await http.PostAsJsonAsync(url, data);
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var refreshed = await TryRefreshAsync();
            if (refreshed)
            {
                http = await CreateClientWithAuthAsync();
                resp = await http.PostAsJsonAsync(url, data);
            }
        }
        return resp;
    }

    public async Task<HttpResponseMessage> PutAsJsonWithRefreshAsync<T>(string url, T data)
    {
        var http = await CreateClientWithAuthAsync();
        var resp = await http.PutAsJsonAsync(url, data);
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var refreshed = await TryRefreshAsync();
            if (refreshed)
            {
                http = await CreateClientWithAuthAsync();
                resp = await http.PutAsJsonAsync(url, data);
            }
        }
        return resp;
    }

    public async Task<bool> TryRefreshAsync()
    {
        var refresh = await _js.InvokeAsync<string?>("localStorage.getItem", "refreshToken");
        if (string.IsNullOrWhiteSpace(refresh)) return false;
        var http = await CreateBaseClientAsync();
        var res = await http.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refresh });
        if (!res.IsSuccessStatusCode) return false;
        var json = await res.Content.ReadFromJsonAsync<TokenPair>();
        if (json == null) return false;
        await _js.InvokeVoidAsync("localStorage.setItem", "accessToken", json.accessToken);
        await _js.InvokeVoidAsync("localStorage.setItem", "refreshToken", json.refreshToken);
        return true;
    }

    private record TokenPair(string accessToken, string refreshToken);

    public async Task<HttpResponseMessage> SendWithRefreshAsync(HttpRequestMessage request)
    {
        var http = await CreateClientWithAuthAsync();
        var resp = await http.SendAsync(request);
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var refreshed = await TryRefreshAsync();
            if (refreshed)
            {
                http = await CreateClientWithAuthAsync();
                // need to clone the request
                var clone = await CloneAsync(request);
                resp = await http.SendAsync(clone);
            }
        }
        return resp;
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        // copy content
        if (request.Content != null)
        {
            var ms = new System.IO.MemoryStream();
            await request.Content.CopyToAsync(ms);
            ms.Position = 0;
            var content = new StreamContent(ms);
            foreach (var h in request.Content.Headers)
                content.Headers.TryAddWithoutValidation(h.Key, h.Value);
            clone.Content = content;
        }
        // copy headers
        foreach (var h in request.Headers)
            clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
        clone.Version = request.Version;
        return clone;
    }
}
