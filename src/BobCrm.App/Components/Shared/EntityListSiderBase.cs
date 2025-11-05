using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using BobCrm.App.Services;

namespace BobCrm.App.Components.Shared;

/// <summary>
/// 实体列表侧边栏基类
/// 提供通用的搜索、加载、过滤逻辑，子类只需实现特定的数据获取和渲染
/// </summary>
public abstract class EntityListSiderBase : ComponentBase, IDisposable
{
    [Inject] protected AuthService Auth { get; set; } = null!;
    [Inject] protected NavigationManager Nav { get; set; } = null!;
    [Inject] protected IJSRuntime JS { get; set; } = null!;
    [Inject] protected I18nService I18n { get; set; } = null!;

    protected string keyword = string.Empty;
    protected string? error;
    protected bool loading = true;

    // ===== 子类需要实现的抽象方法 =====

    /// <summary>API端点</summary>
    protected abstract string ApiEndpoint { get; }

    /// <summary>新建按钮文本的翻译键</summary>
    protected abstract string NewButtonTextKey { get; }

    /// <summary>搜索占位符文本的翻译键</summary>
    protected abstract string SearchPlaceholderKey { get; }

    /// <summary>新建按钮点击路径</summary>
    protected abstract string CreateNewPath { get; }

    /// <summary>获取项目点击时的导航路径</summary>
    protected abstract string GetItemPath(int id);

    /// <summary>注册跨组件事件的JS方法（可选）</summary>
    protected virtual string? RegisterEventsJsMethod => null;

    // ===== 通用的生命周期方法 =====

    protected override void OnInitialized()
    {
        I18n.OnChanged += HandleI18nChanged;
        Nav.LocationChanged += HandleLocationChanged;
    }

    protected virtual void HandleI18nChanged()
    {
        try { InvokeAsync(StateHasChanged); } catch { }
    }

    protected virtual void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        InvokeAsync(StateHasChanged);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        try
        {
            await JS.InvokeVoidAsync("console.log", $"[{GetType().Name}] OnAfterRenderAsync started");

            // 认证检查
            var isAuthenticated = await Auth.EnsureAuthenticatedAsync();
            if (!isAuthenticated)
            {
                await JS.InvokeVoidAsync("console.warn", $"[{GetType().Name}] Not authenticated, redirecting to login");
                Nav.NavigateTo("/login", forceLoad: true);
                return;
            }

            // 注册跨组件事件（如果需要）
            if (!string.IsNullOrWhiteSpace(RegisterEventsJsMethod))
            {
                await JS.InvokeVoidAsync(RegisterEventsJsMethod, DotNetObjectReference.Create(this));
            }

            // 加载国际化
            var saved = await JS.InvokeAsync<string?>("bobcrm.getCookie", "lang");
            var langToLoad = !string.IsNullOrWhiteSpace(saved) ? saved! : "ja";
            await I18n.LoadAsync(langToLoad);
            await InvokeAsync(StateHasChanged);

            // 加载数据
            await LoadDataAsync();

            loading = false;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            error = $"Error: {ex.Message}";
            loading = false;
            await JS.InvokeVoidAsync("console.error", $"[{GetType().Name}] Exception: {ex.Message}");
            StateHasChanged();
        }
    }

    // ===== 通用的业务方法 =====

    /// <summary>加载数据（子类可重写以自定义数据加载逻辑）</summary>
    protected virtual async Task LoadDataAsync()
    {
        await JS.InvokeVoidAsync("console.log", $"[{GetType().Name}] Loading data from {ApiEndpoint}...");
        var resp = await Auth.GetWithRefreshAsync(ApiEndpoint);
        await JS.InvokeVoidAsync("console.log", $"[{GetType().Name}] Response status: {(int)resp.StatusCode}");

        if (resp.IsSuccessStatusCode)
        {
            await OnDataLoadedAsync(resp);
        }
        else
        {
            error = I18n.T("ERR_LOGIN_EXPIRED");
            await JS.InvokeVoidAsync("console.error", $"[{GetType().Name}] Error: {error}");
        }
    }

    /// <summary>数据加载成功后的回调（子类必须实现）</summary>
    protected abstract Task OnDataLoadedAsync(HttpResponseMessage response);

    /// <summary>点击项目时的导航</summary>
    protected virtual void Go(int id)
    {
        Nav.NavigateTo(GetItemPath(id));
    }

    /// <summary>点击新建按钮</summary>
    protected virtual void CreateNew()
    {
        Nav.NavigateTo(CreateNewPath);
    }

    public virtual void Dispose()
    {
        I18n.OnChanged -= HandleI18nChanged;
        Nav.LocationChanged -= HandleLocationChanged;
    }
}

