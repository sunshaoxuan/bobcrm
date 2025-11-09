using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace BobCrm.App.Services;

public class AuthService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IJSRuntime _js;
    
    // 单飞刷新机制：确保同一时间只有一个刷新请求
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private Task<bool>? _refreshTask;
    private DateTime _lastRefreshTime = DateTime.MinValue;

    /// <summary>
    /// 当认证失败且无法刷新时触发（用于全局导航到登录页）
    /// </summary>
    public event Action? OnUnauthorized;

    public AuthService(IHttpClientFactory httpFactory, IJSRuntime js)
    {
        _httpFactory = httpFactory;
        _js = js;
    }

    private async Task<HttpClient> CreateBaseClientAsync()
    {
        var http = _httpFactory.CreateClient("api");
        string? resolvedBase = null;

        // Prefer localStorage apiBase (来自 Setup 保存)，其次 cookie
        try
        {
            var lsBase = await _js.InvokeAsync<string?>("localStorage.getItem", "apiBase");
            if (!string.IsNullOrWhiteSpace(lsBase))
            {
                resolvedBase = NormalizeBase(lsBase!);
            }
            else
            {
                var cookieBase = await _js.InvokeAsync<string?>("bobcrm.getCookie", "apiBase");
                if (!string.IsNullOrWhiteSpace(cookieBase))
                {
                    resolvedBase = NormalizeBase(cookieBase!);
                    // 同步回写，避免再次回退
                    try { await _js.InvokeVoidAsync("localStorage.setItem", "apiBase", resolvedBase); } catch { }
                    try { await _js.InvokeVoidAsync("bobcrm.setCookie", "apiBase", resolvedBase, 365); } catch { }
                }
            }
        }
        catch { }

        // 如果配置已经设置了 BaseAddress，就不要再强行覆盖成前端 Origin；
        // 只有当配置为空时，才回退到浏览器 Origin，避免出现“前端调用自己”导致死循环。
        if (string.IsNullOrWhiteSpace(resolvedBase) && http.BaseAddress == null)
        {
            try
            {
                var origin = await _js.InvokeAsync<string?>("bobcrm.getOrigin");
                if (!string.IsNullOrWhiteSpace(origin))
                {
                    resolvedBase = NormalizeBase(origin!);
                }
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(resolvedBase))
        {
            try
            {
                http.BaseAddress = new Uri(resolvedBase, UriKind.Absolute);
            }
            catch { }
        }
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
                var newToken = await _js.InvokeAsync<string?>("localStorage.getItem", "accessToken");
                if (!string.IsNullOrWhiteSpace(newToken))
                {
                    try
                    {
                        await _js.InvokeVoidAsync("console.log", $"[Auth] Race recovery - retry {methodName}");
                    }
                    catch { }
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
            if ((DateTime.UtcNow - _lastRefreshTime).TotalSeconds < 1)
            {
                try
                {
                    await _js.InvokeVoidAsync("console.log", "[Auth] Skip refresh - recent refresh detected");
                }
                catch { }
                return true;
            }

            // 如果有正在进行的刷新任务，等待它
            if (_refreshTask != null && !_refreshTask.IsCompleted)
            {
                try
                {
                    await _js.InvokeVoidAsync("console.log", "[Auth] Waiting for in-progress refresh");
                }
                catch { }
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
        try
        {
            await _js.InvokeVoidAsync("console.log", "[Auth] Starting token refresh");
        }
        catch { }

        var refresh = await _js.InvokeAsync<string?>("localStorage.getItem", "refreshToken");
        if (string.IsNullOrWhiteSpace(refresh))
        {
            try
            {
                await _js.InvokeVoidAsync("console.warn", "[Auth] No refresh token found");
            }
            catch { }
            await ClearTokensAsync();
            return false;
        }
        
        var http = await CreateBaseClientAsync();
        var res = await http.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refresh });
        
        if (!res.IsSuccessStatusCode)
        {
            // 刷新失败，清理本地 token
            try
            {
                await _js.InvokeVoidAsync("console.error", $"[Auth] Refresh failed: {res.StatusCode}");
            }
            catch { }
            await ClearTokensAsync();
            OnUnauthorized?.Invoke();
            return false;
        }
        
        var json = await res.Content.ReadFromJsonAsync<TokenPair>();
        if (json == null)
        {
            try
            {
                await _js.InvokeVoidAsync("console.error", "[Auth] Refresh response parse failed");
            }
            catch { }
            await ClearTokensAsync();
            return false;
        }
        
        await _js.InvokeVoidAsync("localStorage.setItem", "accessToken", json.accessToken);
        await _js.InvokeVoidAsync("localStorage.setItem", "refreshToken", json.refreshToken);
        _lastRefreshTime = DateTime.UtcNow;
        
        try
        {
            await _js.InvokeVoidAsync("console.log", "[Auth] Token refresh successful");
        }
        catch { }
        
        return true;
    }

    /// <summary>
    /// 清理本地存储的认证令牌
    /// </summary>
    public async Task ClearTokensAsync()
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.removeItem", "accessToken");
            await _js.InvokeVoidAsync("localStorage.removeItem", "refreshToken");
        }
        catch { }
    }

    /// <summary>
    /// 检查用户是否已登录（有有效的 accessToken 或可刷新的 refreshToken）
    /// </summary>
    public async Task<bool> IsSignedInAsync()
    {
        try
        {
            var access = await _js.InvokeAsync<string?>("localStorage.getItem", "accessToken");
            if (!string.IsNullOrWhiteSpace(access))
                return true;

            var refresh = await _js.InvokeAsync<string?>("localStorage.getItem", "refreshToken");
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
            var access = await _js.InvokeAsync<string?>("localStorage.getItem", "accessToken");
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
