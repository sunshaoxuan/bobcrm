using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace BobCrm.App.Services;

public class PreferencesService
{
    private readonly AuthService _auth;
    private readonly IJSRuntime _js;

    public PreferencesService(AuthService auth, IJSRuntime js)
    {
        _auth = auth;
        _js = js;
    }

    public async Task<UserPreferences> LoadPreferencesAsync()
    {
        // Try to load from server first
        try
        {
            var resp = await _auth.GetWithRefreshAsync("/api/user/preferences");
            if (resp.IsSuccessStatusCode)
            {
                var serverPrefs = await resp.Content.ReadFromJsonAsync<UserPreferences>();
                if (serverPrefs != null)
                {
                    // Sync server preferences to localStorage
                    await SyncToLocalStorageAsync(serverPrefs);
                    return serverPrefs;
                }
            }
        }
        catch
        {
            // If server fails, fall back to localStorage
        }

        // Fall back to localStorage
        return await LoadFromLocalStorageAsync();
    }

    public async Task<bool> SavePreferencesAsync(UserPreferences prefs)
    {
        // Save to localStorage immediately
        await SyncToLocalStorageAsync(prefs);

        // Try to save to server
        try
        {
            var dto = new UserPreferencesDto(prefs.Theme, prefs.UdfColor, prefs.Language);
            var resp = await _auth.PutAsJsonWithRefreshAsync("/api/user/preferences", dto);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<UserPreferences> LoadFromLocalStorageAsync()
    {
        try
        {
            // Get system default color from JS (APP_DEFAULTS or CSS var)
            var initColor = await _js.InvokeAsync<string>("bobcrm.getInitColor");
            var theme = await _js.InvokeAsync<string?>("localStorage.getItem", "theme") ?? "light";

            // Get user-defined color from localStorage
            var udfColor = await _js.InvokeAsync<string?>("localStorage.getItem", "udfColor");
            if (string.IsNullOrEmpty(udfColor))
            {
                udfColor = initColor;
                await _js.InvokeVoidAsync("localStorage.setItem", "udfColor", udfColor);
            }

            var language = await _js.InvokeAsync<string?>("bobcrm.getCookie", "lang") ?? "ja";

            return new UserPreferences
            {
                Theme = theme,
                UdfColor = udfColor,
                Language = language
            };
        }
        catch
        {
            string fallbackInit;
            try { fallbackInit = await _js.InvokeAsync<string>("bobcrm.getInitColor"); }
            catch { fallbackInit = string.Empty; }
            return new UserPreferences
            {
                Theme = "light",
                UdfColor = fallbackInit,
                Language = "ja"
            };
        }
    }

    private async Task SyncToLocalStorageAsync(UserPreferences prefs)
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", "theme", prefs.Theme ?? "light");
            var udfColor = prefs.UdfColor ?? await _js.InvokeAsync<string>("bobcrm.getInitColor");
            // 保存到 localStorage（使用 udfColor 键名）
            await _js.InvokeVoidAsync("localStorage.setItem", "udfColor", udfColor);
            if (!string.IsNullOrEmpty(prefs.Language))
            {
                await _js.InvokeVoidAsync("bobcrm.setCookie", "lang", prefs.Language, 365);
            }
        }
        catch
        {
            // Ignore localStorage errors
        }
    }

    public class UserPreferences
    {
        public string? Theme { get; set; }
        public string? UdfColor { get; set; }  // 用户自定义颜色（前端缓存和前后端交互都用此名）
        public string? Language { get; set; }
    }

    private record UserPreferencesDto(string? theme, string? udfColor, string? language);
}
