using System.Net.Http.Json;
using Microsoft.JSInterop;

namespace BobCrm.App.Services;

public class PreferencesService
{
    private const string DefaultTheme = "calm-light";
    private const string DefaultPrimary = "#739FD6";
    private const string DefaultLanguage = "ja";

    private readonly AuthService _auth;
    private readonly IJSRuntime _js;

    public PreferencesService(AuthService auth, IJSRuntime js)
    {
        _auth = auth;
        _js = js;
    }

    public async Task<UserPreferences> LoadPreferencesAsync()
    {
        try
        {
            var resp = await _auth.GetWithRefreshAsync("/api/user/preferences");
            if (resp.IsSuccessStatusCode)
            {
                var serverPrefs = await resp.Content.ReadFromJsonAsync<UserPreferences>();
                if (serverPrefs != null)
                {
                    await SyncToLocalStorageAsync(serverPrefs);
                    return Normalize(serverPrefs);
                }
            }
        }
        catch
        {
            // ignore network errors, fall back to local data
        }

        return await LoadFromLocalStorageAsync();
    }

    public async Task<bool> SavePreferencesAsync(UserPreferences prefs)
    {
        var normalized = Normalize(prefs);
        await SyncToLocalStorageAsync(normalized);

        try
        {
            var dto = new UserPreferencesDto(normalized.Theme, normalized.UdfColor, normalized.Language);
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
            var language = await _js.InvokeAsync<string?>("bobcrm.getCookie", "lang") ?? DefaultLanguage;
            return new UserPreferences
            {
                Theme = DefaultTheme,
                UdfColor = DefaultPrimary,
                Language = language
            };
        }
        catch
        {
            return new UserPreferences
            {
                Theme = DefaultTheme,
                UdfColor = DefaultPrimary,
                Language = DefaultLanguage
            };
        }
    }

    private async Task SyncToLocalStorageAsync(UserPreferences prefs)
    {
        try
        {
            if (!string.IsNullOrEmpty(prefs.Language))
            {
                await _js.InvokeVoidAsync("bobcrm.setCookie", "lang", prefs.Language, 365);
            }
        }
        catch
        {
            // Ignore client persistence errors
        }
    }

    private static UserPreferences Normalize(UserPreferences prefs)
    {
        return new UserPreferences
        {
            Theme = string.IsNullOrWhiteSpace(prefs.Theme) ? DefaultTheme : prefs.Theme,
            UdfColor = string.IsNullOrWhiteSpace(prefs.UdfColor) ? DefaultPrimary : prefs.UdfColor,
            Language = string.IsNullOrWhiteSpace(prefs.Language) ? DefaultLanguage : prefs.Language
        };
    }

    public class UserPreferences
    {
        public string? Theme { get; set; }
        public string? UdfColor { get; set; }
        public string? Language { get; set; }
    }

    private record UserPreferencesDto(string? theme, string? udfColor, string? language);
}
