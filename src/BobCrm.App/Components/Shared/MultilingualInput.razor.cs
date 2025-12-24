using AntDesign;
using BobCrm.App.Models;
using BobCrm.App.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace BobCrm.App.Components.Shared;

public partial class MultilingualInput : IAsyncDisposable
{
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private I18nService I18n { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ILogger<MultilingualInput> Logger { get; set; } = default!;

    [Parameter] public MultilingualTextDto? Value { get; set; }
    [Parameter] public EventCallback<MultilingualTextDto?> ValueChanged { get; set; }
    [Parameter] public string? DefaultLanguage { get; set; }

    private List<LanguageInfo>? _languages;
    private readonly Dictionary<string, string?> _values = new();
    private bool _isExpanded;
    private string _defaultLanguage = "ja";
    private ElementReference _triggerRef;
    private string _overlayStyle = string.Empty;
    private IJSObjectReference? _module;
    private DotNetObjectReference<MultilingualInput>? _dotNetRef;
    private double? _listenerToken;

    private class LanguageInfo
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    private record OverlayPosition(double Top, double Left, double MinWidth);

    private class ApiLanguageDto
    {
        public string code { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
    }

    protected override async Task OnInitializedAsync()
    {
        _defaultLanguage = DefaultLanguage?.ToLowerInvariant() ?? I18n.CurrentLang.ToLowerInvariant();
        I18n.OnChanged += HandleLanguageChanged;
        _dotNetRef = DotNetObjectReference.Create(this);

        try
        {
            var apiLanguages = await Http.GetFromJsonAsync<List<ApiLanguageDto>>("/api/i18n/languages");
            if (apiLanguages == null || apiLanguages.Count == 0)
            {
                throw new InvalidOperationException("Languages not returned");
            }

            _languages = apiLanguages
                .Select(l => new LanguageInfo { Code = l.code.ToLowerInvariant(), Name = l.name })
                .OrderBy(l => l.Code != _defaultLanguage)
                .ThenBy(l => l.Name)
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "[MultilingualInput] Failed to load languages");
            _languages = new List<LanguageInfo>
            {
                new() { Code = "ja", Name = "日本語" },
                new() { Code = "zh", Name = "中文" },
                new() { Code = "en", Name = "English" }
            };
        }

        InitializeValues();
    }

    protected override void OnParametersSet()
    {
        InitializeValues();
    }

    private void InitializeValues()
    {
        if (_languages == null)
        {
            return;
        }

        foreach (var lang in _languages)
        {
            _values[lang.Code] = Value?.GetValue(lang.Code);
        }
    }

    private void HandleLanguageChanged()
    {
        if (DefaultLanguage != null)
        {
            return;
        }

        var newLang = I18n.CurrentLang.ToLowerInvariant();
        if (newLang == _defaultLanguage)
        {
            return;
        }

        _defaultLanguage = newLang;
        if (_languages != null && _languages.Count > 0)
        {
            _languages = _languages
                .OrderBy(l => l.Code != _defaultLanguage)
                .ThenBy(l => l.Name)
                .ToList();
        }

        InvokeAsync(StateHasChanged);
    }

    private string? GetDefaultValue()
    {
        if (_languages == null)
        {
            return null;
        }

        return Value?.GetValue(_defaultLanguage) ?? Value?.FirstOrDefault().Value;
    }

    private string GetDefaultPlaceholder() => I18n.T("TXT_MULTILINGUAL_PLACEHOLDER");

    private string GetPlaceholder(string langCode) =>
        string.Equals(langCode, _defaultLanguage, StringComparison.OrdinalIgnoreCase)
            ? I18n.T("TXT_MULTILINGUAL_PLACEHOLDER_DEFAULT")
            : I18n.T("TXT_MULTILINGUAL_PLACEHOLDER_OTHER");

    private Task OnValueChanged(string lang, string? newValue)
    {
        _values[lang] = newValue;

        var dto = Value ?? new MultilingualTextDto();
        dto[lang] = newValue;
        return ValueChanged.InvokeAsync(dto);
    }

    private RenderFragment RenderOverlay() => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "multilingual-overlay");

        if (_languages == null || !_languages.Any())
        {
            builder.OpenElement(2, "div");
            builder.AddAttribute(3, "class", "multilingual-loading");
            builder.OpenComponent<Spin>(4);
            builder.AddAttribute(5, "Size", "small");
            builder.CloseComponent();
            builder.CloseElement();
        }
        else
        {
            foreach (var lang in _languages)
            {
                var isDefault = string.Equals(lang.Code, _defaultLanguage, StringComparison.OrdinalIgnoreCase);

                builder.OpenElement(6, "div");
                builder.AddAttribute(7, "class", $"multilingual-input-row {(isDefault ? "default-language" : string.Empty)}");

                builder.OpenElement(8, "span");
                builder.AddAttribute(9, "class", "language-label");
                builder.AddAttribute(10, "title", isDefault ? I18n.T("LBL_DEFAULT_LANGUAGE") : string.Empty);
                builder.AddContent(11, lang.Name);
                if (isDefault)
                {
                    builder.OpenComponent<Icon>(12);
                    builder.AddAttribute(13, "Type", "star");
                    builder.AddAttribute(14, "Theme", IconThemeType.Fill);
                    builder.AddAttribute(15, "Style", "font-size: 10px; color: #faad14; margin-left: 4px;");
                    builder.CloseComponent();
                }

                builder.CloseElement(); // span

                builder.OpenComponent<Input<string>>(16);
                builder.AddAttribute(17, "Value", _values.TryGetValue(lang.Code, out var val) ? val : null);
                builder.AddAttribute(18, "ValueChanged", EventCallback.Factory.Create<string?>(this, v => OnValueChanged(lang.Code, v)));
                builder.AddAttribute(19, "Placeholder", GetPlaceholder(lang.Code));
                builder.AddAttribute(20, "Class", "field-control multilingual-field");
                builder.CloseComponent();

                builder.CloseElement(); // row
            }
        }

        builder.CloseElement(); // overlay
    };

    private async Task TogglePopoverAsync()
    {
        if (_isExpanded)
        {
            await ClosePopoverAsync();
        }
        else
        {
            await OpenPopoverAsync();
        }
    }

    private async Task OpenPopoverAsync()
    {
        await EnsureModuleAsync();

        _isExpanded = true;
        await UpdateOverlayPositionAsync();
        _listenerToken = await _module!.InvokeAsync<double>("multilingualInput_registerHandlers", _dotNetRef);
        await InvokeAsync(StateHasChanged);
    }

    private async Task ClosePopoverAsync()
    {
        if (!_isExpanded)
        {
            return;
        }

        _isExpanded = false;
        if (_listenerToken.HasValue && _module != null)
        {
            await _module.InvokeVoidAsync("multilingualInput_removeHandlers", _listenerToken.Value);
            _listenerToken = null;
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task UpdateOverlayPositionAsync()
    {
        if (_module == null)
        {
            return;
        }

        var estimatedHeight = Math.Min(360, Math.Max(1, (_languages?.Count ?? 0)) * 52);
        var pos = await _module.InvokeAsync<OverlayPosition>("multilingualInput_getPosition", _triggerRef, estimatedHeight);
        _overlayStyle = $"top:{pos.Top}px;left:{pos.Left}px;min-width:{pos.MinWidth}px;";
    }

    [JSInvokable]
    public Task CloseFromJs() => ClosePopoverAsync();

    private Task CloseBackdrop() => ClosePopoverAsync();

    private async Task EnsureModuleAsync()
    {
        if (_module == null)
        {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/multilingual-input.js");
        }
    }

    public async ValueTask DisposeAsync()
    {
        I18n.OnChanged -= HandleLanguageChanged;

        if (_listenerToken.HasValue && _module != null)
        {
            await _module.InvokeVoidAsync("multilingualInput_removeHandlers", _listenerToken.Value);
        }

        if (_module != null)
        {
            await _module.DisposeAsync();
        }

        _dotNetRef?.Dispose();
    }
}
