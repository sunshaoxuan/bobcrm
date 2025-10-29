using System.Text.Json;

namespace BobCrm.App.Services;

public class I18nService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly AuthService _auth;
    private Dictionary<string, string> _dict = new(StringComparer.OrdinalIgnoreCase);
    public string CurrentLang { get; private set; } = "zh";
    public event Action? OnChanged;

    public I18nService(IHttpClientFactory httpFactory, AuthService auth)
    { _httpFactory = httpFactory; _auth = auth; }

    public string T(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return string.Empty;
        return _dict.TryGetValue(key, out var v) ? v : key;
    }

    public async Task LoadAsync(string lang, CancellationToken ct = default)
    {
        lang = (lang ?? "zh").ToLowerInvariant();
        var http = await _auth.CreateClientWithAuthAsync();
        var resp = await http.GetAsync($"/api/i18n/{lang}", ct);
        if (!resp.IsSuccessStatusCode) return;
        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in doc.RootElement.EnumerateObject())
        {
            map[p.Name] = p.Value.GetString() ?? p.Name;
        }
        _dict = map; CurrentLang = lang; OnChanged?.Invoke();
    }
}

