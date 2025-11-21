using System;
using System.Threading;
using System.Threading.Tasks;
using BobCrm.Api.Abstractions;

namespace BobCrm.Api.Services;

/// <summary>
/// Lightweight I18n service used by the API to provide translations at build/test time.
/// Keeps API composition working without requiring the Blazor-side implementation.
/// </summary>
public class DefaultI18nService : II18nService
{
    public string CurrentLang { get; private set; } = "en";

    public event Action? OnChanged;

    public Task LoadAsync(string lang, bool force = false, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(lang) &&
            (force || !string.Equals(CurrentLang, lang, StringComparison.OrdinalIgnoreCase)))
        {
            CurrentLang = lang;
            OnChanged?.Invoke();
        }

        return Task.CompletedTask;
    }

    public string T(string key) => key;
}
