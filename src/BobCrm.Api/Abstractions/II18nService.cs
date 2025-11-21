using System;
using System.Threading;
using System.Threading.Tasks;

namespace BobCrm.Api.Abstractions;

public interface II18nService
{
    string T(string key);
    Task LoadAsync(string lang, bool force = false, CancellationToken ct = default);
    string CurrentLang { get; }
    event Action? OnChanged;
}
