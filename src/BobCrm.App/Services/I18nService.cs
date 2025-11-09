using Microsoft.JSInterop;
using System.Text.Json;

namespace BobCrm.App.Services;

public class I18nService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly AuthService _auth;
    private readonly IJSRuntime _js;
    private Dictionary<string, string> _dict = new(StringComparer.OrdinalIgnoreCase);
    public string CurrentLang { get; private set; } = "ja";
    public bool IsLoaded => _dict.Count > 0;
    public event Action? OnChanged;

    public I18nService(IHttpClientFactory httpFactory, AuthService auth, IJSRuntime js)
    {
        _httpFactory = httpFactory; _auth = auth; _js = js;
    }

    public string T(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        return _dict.TryGetValue(key, out var v) ? v : key;
    }

    public async Task LoadAsync(string lang, CancellationToken ct = default)
    {
        lang = (lang ?? "ja").ToLowerInvariant();
        try
        {
            var http = await _auth.CreateClientWithLangAsync();
            using var resp = await http.GetAsync($"/api/i18n/{lang}", ct);
            if (!resp.IsSuccessStatusCode)
            {
                await HandleLoadFailureAsync(lang);
                return;
            }
            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                map[p.Name] = p.Value.GetString() ?? p.Name;
            }
            _dict = map;
            CurrentLang = lang;
            OnChanged?.Invoke();
            try { await _js.InvokeVoidAsync("bobcrm.setLang", lang); } catch { }
            try { await _js.InvokeVoidAsync("bobcrm.setCookie", "lang", lang, 365); } catch { }
        }
        catch
        {
            await HandleLoadFailureAsync(lang);
        }
    }

    private Task HandleLoadFailureAsync(string lang)
    {
        // 仍然触发 OnChanged，让外层 UI 不至于永久卡在“正在准备”状态
        _dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        CurrentLang = lang;
        OnChanged?.Invoke();
        return Task.CompletedTask;
    }
}
