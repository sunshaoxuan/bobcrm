using Microsoft.JSInterop;

namespace BobCrm.App.Services;

/// <summary>
/// 全局主题状态：负责记录当前主题（Calm Light/Dark）并同步到 DOM。
/// </summary>
public class ThemeState
{
    public const string CalmLight = "theme-calm-light";
    public const string CalmDark = "theme-calm-dark";

    private readonly IJSRuntime _jsRuntime;
    private string _currentTheme = CalmLight;
    private bool _domInitialized;

    public ThemeState(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public string CurrentTheme => _currentTheme;

    public event Action? OnChanged;

    public async Task EnsureAppliedAsync()
    {
        if (_domInitialized)
        {
            return;
        }

        await ApplyToDomAsync(_currentTheme);
        _domInitialized = true;
    }

    public async Task SetThemeAsync(string? theme)
    {
        var normalized = Normalize(theme);
        if (string.Equals(_currentTheme, normalized, StringComparison.OrdinalIgnoreCase) && _domInitialized)
        {
            return;
        }

        _currentTheme = normalized;

        if (_domInitialized)
        {
            await ApplyToDomAsync(_currentTheme);
        }
        else
        {
            await EnsureAppliedAsync();
        }

        OnChanged?.Invoke();
    }

    private static string Normalize(string? theme)
    {
        return theme?.Trim() switch
        {
            CalmDark => CalmDark,
            _ => CalmLight
        };
    }

    private async Task ApplyToDomAsync(string theme)
    {
        await _jsRuntime.InvokeVoidAsync("bobcrmTheme.apply", theme);
    }
}
