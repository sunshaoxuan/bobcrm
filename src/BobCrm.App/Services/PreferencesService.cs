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

    public static LayoutState.NavDisplayMode ToLayoutMode(string? navMode) =>
        navMode?.ToLowerInvariant() switch
        {
            "icons" => LayoutState.NavDisplayMode.Icons,
            "labels" => LayoutState.NavDisplayMode.Labels,
            _ => LayoutState.NavDisplayMode.IconText
        };

    public static string ToApiNavMode(LayoutState.NavDisplayMode mode) =>
        mode switch
        {
            LayoutState.NavDisplayMode.Icons => "icons",
            LayoutState.NavDisplayMode.Labels => "labels",
            _ => "icon-text"
        };

    #region DTOs

    public class UserSettingsSnapshot
    {
        [JsonPropertyName("system")]
        public SystemSettingsDto System { get; set; } = new();

        [JsonPropertyName("effective")]
        public UserSettingsDto Effective { get; set; } = new();

        [JsonPropertyName("overrides")]
        public UserSettingsDto? Overrides { get; set; }
    }

    public class SystemSettingsDto
    {
        [JsonPropertyName("companyName")]
        public string CompanyName { get; set; } = "OneCRM";

        [JsonPropertyName("defaultTheme")]
        public string DefaultTheme { get; set; } = "calm-light";

        [JsonPropertyName("defaultPrimaryColor")]
        public string? DefaultPrimaryColor { get; set; } = "#739FD6";

        [JsonPropertyName("defaultLanguage")]
        public string DefaultLanguage { get; set; } = "ja";

        [JsonPropertyName("defaultHomeRoute")]
        public string DefaultHomeRoute { get; set; } = "/";

        [JsonPropertyName("defaultNavDisplayMode")]
        public string DefaultNavDisplayMode { get; set; } = "icon-text";

        [JsonPropertyName("timeZoneId")]
        public string TimeZoneId { get; set; } = "Asia/Tokyo";

        [JsonPropertyName("allowSelfRegistration")]
        public bool AllowSelfRegistration { get; set; }
    }

    public class UserSettingsDto
    {
        [JsonPropertyName("theme")]
        public string Theme { get; set; } = "calm-light";

        [JsonPropertyName("primaryColor")]
        public string? PrimaryColor { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; } = "ja";

        [JsonPropertyName("homeRoute")]
        public string HomeRoute { get; set; } = "/";

        [JsonPropertyName("navDisplayMode")]
        public string NavDisplayMode { get; set; } = "icon-text";
    }

    public class UpdateUserSettingsRequest
    {
        [JsonPropertyName("theme")]
        public string? Theme { get; set; }

        [JsonPropertyName("primaryColor")]
        public string? PrimaryColor { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("homeRoute")]
        public string? HomeRoute { get; set; }

        [JsonPropertyName("navDisplayMode")]
        public string? NavDisplayMode { get; set; }

        [JsonIgnore]
        public bool IsEmpty =>
            Theme is null &&
            PrimaryColor is null &&
            Language is null &&
            HomeRoute is null &&
            NavDisplayMode is null;
    }

    public class UpdateSystemSettingsRequest
    {
        [JsonPropertyName("companyName")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("defaultTheme")]
        public string? DefaultTheme { get; set; }

        [JsonPropertyName("defaultPrimaryColor")]
        public string? DefaultPrimaryColor { get; set; }

        [JsonPropertyName("defaultLanguage")]
        public string? DefaultLanguage { get; set; }

        [JsonPropertyName("defaultHomeRoute")]
        public string? DefaultHomeRoute { get; set; }

        [JsonPropertyName("defaultNavDisplayMode")]
        public string? DefaultNavDisplayMode { get; set; }

        [JsonPropertyName("timeZoneId")]
        public string? TimeZoneId { get; set; }

        [JsonPropertyName("allowSelfRegistration")]
        public bool? AllowSelfRegistration { get; set; }
    }

    #endregion
}
