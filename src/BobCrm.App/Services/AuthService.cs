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

    public async Task<HttpClient> CreateClientWithAuthAsync()
    {
        var http = _httpFactory.CreateClient("api");
        var access = await _js.InvokeAsync<string?>("localStorage.getItem", "accessToken");
        if (!string.IsNullOrWhiteSpace(access))
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
        }
        return http;
    }

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

    public async Task<bool> TryRefreshAsync()
    {
        var refresh = await _js.InvokeAsync<string?>("localStorage.getItem", "refreshToken");
        if (string.IsNullOrWhiteSpace(refresh)) return false;
        var http = _httpFactory.CreateClient("api");
        var res = await http.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refresh });
        if (!res.IsSuccessStatusCode) return false;
        var json = await res.Content.ReadFromJsonAsync<TokenPair>();
        if (json == null) return false;
        await _js.InvokeVoidAsync("localStorage.setItem", "accessToken", json.accessToken);
        await _js.InvokeVoidAsync("localStorage.setItem", "refreshToken", json.refreshToken);
        return true;
    }

    private record TokenPair(string accessToken, string refreshToken);
}

