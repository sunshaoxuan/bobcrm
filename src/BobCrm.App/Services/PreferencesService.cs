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
            var dto = new UserPreferencesDto(prefs.Theme, prefs.PrimaryColor, prefs.Language);
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
            var theme = await _js.InvokeAsync<string?>("localStorage.getItem", "theme") ?? "light";
            var primaryColor = await _js.InvokeAsync<string?>("localStorage.getItem", "primaryColor") ?? "#3f7cff";
            var language = await _js.InvokeAsync<string?>("bobcrm.getCookie", "lang") ?? "ja";

            return new UserPreferences
            {
                Theme = theme,
                PrimaryColor = primaryColor,
                Language = language
            };
        }
        catch
        {
            return new UserPreferences
            {
                Theme = "light",
                PrimaryColor = "#3f7cff",
                Language = "ja"
            };
        }
    }

    private async Task SyncToLocalStorageAsync(UserPreferences prefs)
    {
        try
        {
            await _js.InvokeVoidAsync("localStorage.setItem", "theme", prefs.Theme ?? "light");
            await _js.InvokeVoidAsync("localStorage.setItem", "primaryColor", prefs.PrimaryColor ?? "#3f7cff");
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
        public string? PrimaryColor { get; set; }
        public string? Language { get; set; }
    }

    private record UserPreferencesDto(string? theme, string? primaryColor, string? language);
}
