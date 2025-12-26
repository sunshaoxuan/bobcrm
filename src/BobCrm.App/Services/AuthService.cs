using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Net.Http.Json;
// using BobCrm.App.Services.JsInterop; removed

namespace BobCrm.App.Services;

public class AuthService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IJsInteropService _js;
    private readonly TimeProvider _timeProvider;
    
    // 单飞刷新机制：确保同一时间只有一个刷新请求
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private Task<bool>? _refreshTask;
    private DateTimeOffset _lastRefreshTime = DateTimeOffset.MinValue;

    /// <summary>
    /// 当认证失败且无法刷新时触发（用于全局导航到登录页）
    /// </summary>
    public event Action? OnUnauthorized;

    public AuthService(IHttpClientFactory httpFactory, IJsInteropService js, TimeProvider timeProvider)
    {
        _httpFactory = httpFactory;
        _js = js;
        _timeProvider = timeProvider;
    }

    private async Task<HttpClient> CreateBaseClientAsync()
    {
        var http = _httpFactory.CreateClient("api");
        string? resolvedBase = null;

        // Prefer localStorage apiBase (来自 Setup 保存)，其次 cookie
        try
        {
            var (_, lsBase) = await _js.TryInvokeAsync<string?>("localStorage.getItem", "apiBase");
            if (!string.IsNullOrWhiteSpace(lsBase))
            {
                resolvedBase = NormalizeBase(lsBase!);
            }
            else
            {
                var (_, cookieBase) = await _js.TryInvokeAsync<string?>("bobcrm.getCookie", "apiBase");
                if (!string.IsNullOrWhiteSpace(cookieBase))
                {
                    resolvedBase = NormalizeBase(cookieBase!);
                    // 同步回写，避免再次回退
                    await _js.TryInvokeVoidAsync("localStorage.setItem", "apiBase", resolvedBase);
                    await _js.TryInvokeVoidAsync("bobcrm.setCookie", "apiBase", resolvedBase, 365);
                }
            }
        }
        catch (Exception ex) { Console.WriteLine($"[AuthService] Error resolving base URL: {ex.Message}"); }

        // 如果配置已经设置了 BaseAddress，就不要再强行覆盖成前端 Origin；
        // 只有当配置为空时，才回退到浏览器 Origin，避免出现“前端调用自己”导致死循环。
        if (string.IsNullOrWhiteSpace(resolvedBase) && http.BaseAddress == null)
        {
            try
            {
                var (_, origin) = await _js.TryInvokeAsync<string?>("bobcrm.getOrigin");
                if (!string.IsNullOrWhiteSpace(origin))
                {
                    resolvedBase = NormalizeBase(origin!);
                }
            }
            catch { /* Ignored: Origin retrieval failed */ }
        }

        if (!string.IsNullOrWhiteSpace(resolvedBase))
        {
            try
            {
                http.BaseAddress = new Uri(resolvedBase, UriKind.Absolute);
            }
            catch { /* Ignored: Invalid URI format */ }
        }
        // attach language header (no auth required)
        try
        {
            var (_, lang) = await _js.TryInvokeAsync<string?>("bobcrm.getCookie", "lang");
            lang ??= "ja";
            if (http.DefaultRequestHeaders.Contains("X-Lang"))
                http.DefaultRequestHeaders.Remove("X-Lang");
            http.DefaultRequestHeaders.Add("X-Lang", lang.ToLowerInvariant());
        }
        catch { /* Ignored: Language header setup failed */ }
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
        catch { return baseUrl; /* Use original on error */ }
    }

    public async Task<HttpClient> CreateClientWithAuthAsync()
    {
        var http = await CreateBaseClientAsync();
        var (_, access) = await _js.TryInvokeAsync<string?>("localStorage.getItem", "accessToken");
        if (!string.IsNullOrWhiteSpace(access))
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", access);
        }
        return http;
    }

    /// <summary>
    /// 兼容方法：CreateAuthedClientAsync 作为 CreateClientWithAuthAsync 的别名
    /// </summary>
    public Task<HttpClient> CreateAuthedClientAsync() => CreateClientWithAuthAsync();

    public Task<HttpClient> CreateClientWithLangAsync() => CreateBaseClientAsync();

    /// <summary>
    /// 统一的认证请求发送器：自动处理 401、单飞刷新、竞态重试
    /// </summary>
    private async Task<HttpResponseMessage> SendWithAuthAndRetryAsync(
        Func<HttpClient, Task<HttpResponseMessage>> sendFunc,
        string methodName)
    {
        var http = await CreateClientWithAuthAsync();
        var resp = await sendFunc(http);
        
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            var refreshed = await TryRefreshAsync();
            if (refreshed)
            {
                http = await CreateClientWithAuthAsync();
                resp = await sendFunc(http);
            }
            else
            {
                // 竞态重试：可能刷新已被其他并发请求完成，等待并重试
                await Task.Delay(200);
                var (_, newToken) = await _js.TryInvokeAsync<string?>("localStorage.getItem", "accessToken");
                if (!string.IsNullOrWhiteSpace(newToken))
                {
                    await _js.TryInvokeVoidAsync("console.log", $"[Auth] Race recovery - retry {methodName}");
                    http = await CreateClientWithAuthAsync();
                    resp = await sendFunc(http);
                }
            }
        }
        return resp;
    }

    public Task<HttpResponseMessage> GetWithRefreshAsync(string url) =>
        SendWithAuthAndRetryAsync(client => client.GetAsync(url), "GET");

    public Task<HttpResponseMessage> PostAsJsonWithRefreshAsync<T>(string url, T data) =>
        SendWithAuthAndRetryAsync(client => client.PostAsJsonAsync(url, data), "POST");

    public Task<HttpResponseMessage> PutAsJsonWithRefreshAsync<T>(string url, T data) =>
        SendWithAuthAndRetryAsync(client => client.PutAsJsonAsync(url, data), "PUT");

    public Task<HttpResponseMessage> DeleteWithRefreshAsync(string url) =>
        SendWithAuthAndRetryAsync(client => client.DeleteAsync(url), "DELETE");

    public async Task<bool> TryRefreshAsync()
    {
        // 单飞机制：如果已有刷新任务在进行，等待其完成而不是再发一次
        await _refreshGate.WaitAsync();
        try
        {
            // 双重检查：进入临界区后，如果最近1秒内已成功刷新过，直接返回成功
            if ((_timeProvider.GetUtcNow() - _lastRefreshTime).TotalSeconds < 1)
            {
                await _js.TryInvokeVoidAsync("console.log", "[Auth] Skip refresh - recent refresh detected");
                return true;
            }

            // 如果有正在进行的刷新任务，等待它
            if (_refreshTask != null && !_refreshTask.IsCompleted)
            {
                await _js.TryInvokeVoidAsync("console.log", "[Auth] Waiting for in-progress refresh");
                return await _refreshTask;
            }

            // 创建新的刷新任务
            _refreshTask = PerformRefreshAsync();
            return await _refreshTask;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private async Task<bool> PerformRefreshAsync()
    {
        await _js.TryInvokeVoidAsync("console.log", "[Auth] Starting token refresh");

        var (_, refresh) = await _js.TryInvokeAsync<string?>("localStorage.getItem", "refreshToken");
        if (string.IsNullOrWhiteSpace(refresh))
        {
            await _js.TryInvokeVoidAsync("console.warn", "[Auth] No refresh token found");
            await ClearTokensAsync();
            return false;
        }
        
        var http = await CreateBaseClientAsync();
        var res = await http.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refresh });
        
        if (!res.IsSuccessStatusCode)
        {
            // 刷新失败，清理本地 token
            await _js.TryInvokeVoidAsync("console.error", $"[Auth] Refresh failed: {res.StatusCode}");
            await ClearTokensAsync();
            OnUnauthorized?.Invoke();
            return false;
        }

        var json = await ApiResponseHelper.ReadDataAsync<TokenPair>(res);
        if (json is null)
        {
            await _js.TryInvokeVoidAsync("console.error", "[Auth] Refresh response parse failed");
            await ClearTokensAsync();
            return false;
        }
        
        await _js.TryInvokeVoidAsync("localStorage.setItem", "accessToken", json.accessToken);
        await _js.TryInvokeVoidAsync("localStorage.setItem", "refreshToken", json.refreshToken);
        _lastRefreshTime = _timeProvider.GetUtcNow();
        
        await _js.TryInvokeVoidAsync("console.log", "[Auth] Token refresh successful");
        
        return true;
    }

    /// <summary>
    /// 清理本地存储的认证令牌
    /// </summary>
    public async Task ClearTokensAsync()
    {
        try
        {
            await _js.TryInvokeVoidAsync("localStorage.removeItem", "accessToken");
            await _js.TryInvokeVoidAsync("localStorage.removeItem", "refreshToken");
        }
        catch { /* Ignored: Storage cleanup failed */ }
    }

    /// <summary>
    /// 检查用户是否已登录（有有效的 accessToken 或可刷新的 refreshToken）
    /// </summary>
    public async Task<bool> IsSignedInAsync()
    {
        try
        {
            var (_, access) = await _js.TryInvokeAsync<string?>("localStorage.getItem", "accessToken");
            if (!string.IsNullOrWhiteSpace(access))
                return true;

            var (_, refresh) = await _js.TryInvokeAsync<string?>("localStorage.getItem", "refreshToken");
            return !string.IsNullOrWhiteSpace(refresh);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 确保已认证，如果未认证尝试刷新令牌
    /// </summary>
    /// <returns>true 表示已认证或刷新成功，false 表示需要登录</returns>
    public async Task<bool> EnsureAuthenticatedAsync()
    {
        try
        {
            // 检查是否有 accessToken
            var (_, access) = await _js.TryInvokeAsync<string?>("localStorage.getItem", "accessToken");
            if (!string.IsNullOrWhiteSpace(access))
                return true;

            // 没有 accessToken，尝试刷新
            return await TryRefreshAsync();
        }
        catch
        {
            return false;
        }
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
