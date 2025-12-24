using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace BobCrm.App.Services;

/// <summary>
/// Centralized bridge for system/user settings + local side-effects (lang cookie, nav mode, etc).
/// </summary>
public class PreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AuthService _auth;
    private readonly IJSRuntime _js;
    private UserSettingsSnapshot? _cache;

    public PreferencesService(AuthService auth, IJSRuntime js)
    {
        _auth = auth;
        _js = js;
    }

    public async Task<UserSettingsSnapshot?> LoadSnapshotAsync(bool forceRefresh = false)
    {
        if (!forceRefresh && _cache is not null)
        {
            return _cache;
        }

        try
        {
            var resp = await _auth.GetWithRefreshAsync("/api/settings/user");
            if (!resp.IsSuccessStatusCode)
            {
                return _cache;
            }

            var snapshot = await resp.Content.ReadFromJsonAsync<UserSettingsSnapshot>(JsonOptions);
            if (snapshot is null)
            {
                return _cache;
            }

            await ApplyLocalSideEffectsAsync(snapshot.Effective);
            _cache = snapshot;
            return snapshot;
        }
        catch
        {
            return _cache;
        }
    }

    public async Task<UserSettingsSnapshot?> SaveUserSettingsAsync(UpdateUserSettingsRequest update)
    {
        if (update is null || update.IsEmpty)
        {
            return _cache;
        }

        try
        {
            var resp = await _auth.PutAsJsonWithRefreshAsync("/api/settings/user", update);
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }

            var snapshot = await resp.Content.ReadFromJsonAsync<UserSettingsSnapshot>(JsonOptions);
            if (snapshot is null)
            {
                return null;
            }

            await ApplyLocalSideEffectsAsync(snapshot.Effective);
            _cache = snapshot;
            return snapshot;
        }
        catch
        {
            return null;
        }
    }

    public async Task<SystemSettingsDto?> LoadSystemSettingsAsync()
    {
        try
        {
            var resp = await _auth.GetWithRefreshAsync("/api/settings/system");
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }

            return await resp.Content.ReadFromJsonAsync<SystemSettingsDto>(JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public async Task<SystemSettingsDto?> SaveSystemSettingsAsync(UpdateSystemSettingsRequest update)
    {
        try
        {
            var resp = await _auth.PutAsJsonWithRefreshAsync("/api/settings/system", update);
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }

            return await resp.Content.ReadFromJsonAsync<SystemSettingsDto>(JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private async Task ApplyLocalSideEffectsAsync(UserSettingsDto effective)
    {
        try
        {
            var lang = string.IsNullOrWhiteSpace(effective.Language) ? "ja" : effective.Language;
            await _js.InvokeVoidAsync("bobcrm.setCookie", "lang", lang, 365);
            await _js.InvokeVoidAsync("bobcrm.setLang", lang);
        }
        catch
        {
            // Ignore client language sync failures.
        }

        try
        {
            var navMode = string.IsNullOrWhiteSpace(effective.NavDisplayMode)
                ? "icon-text"
                : effective.NavDisplayMode.ToLowerInvariant();
            await _js.InvokeVoidAsync("localStorage.setItem", "navMode", navMode);
        }
        catch
        {
            // Ignore localStorage errors (Safari private mode, etc.)
        }
    }

    public static NavDisplayMode ToLayoutMode(string? navMode) =>
        navMode?.ToLowerInvariant() switch
        {
            "icons" => NavDisplayMode.Icons,
            "labels" => NavDisplayMode.Labels,
            _ => NavDisplayMode.IconText
        };

    public static string ToApiNavMode(NavDisplayMode mode) =>
        mode switch
        {
            NavDisplayMode.Icons => "icons",
            NavDisplayMode.Labels => "labels",
            _ => "icon-text"
        };

}
