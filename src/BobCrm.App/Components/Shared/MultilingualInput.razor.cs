using AntDesign;
using BobCrm.App.Models;
using BobCrm.App.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace BobCrm.App.Components.Shared;

public partial class MultilingualInput : IAsyncDisposable
{
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private I18nService I18n { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    [Parameter] public MultilingualTextDto? Value { get; set; }
    [Parameter] public EventCallback<MultilingualTextDto?> ValueChanged { get; set; }
    [Parameter] public string? DefaultLanguage { get; set; }

    private List<LanguageInfo>? _languages;
    private Dictionary<string, string?> _values = new();
    private bool _isExpanded = false;
    private string _defaultLanguage = "ja";
    private string _overlayId = $"multilingual-overlay-{Guid.NewGuid():N}";
    private IJSObjectReference? _jsModule;
    private DotNetObjectReference<MultilingualInput>? _dotNetRef;

    private class LanguageInfo
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    private class ApiLanguageDto
    {
        public string code { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
    }

    protected override async Task OnInitializedAsync()
    {
        _defaultLanguage = DefaultLanguage?.ToLowerInvariant() ?? I18n.CurrentLang.ToLowerInvariant();

        // Subscribe to language changes
        I18n.OnChanged += HandleLanguageChanged;

        try
        {
            // 从API获取可用语言列表
            var apiLanguages = await Http.GetFromJsonAsync<List<ApiLanguageDto>>("/api/i18n/languages");
            if (apiLanguages != null && apiLanguages.Any())
            {
                _languages = apiLanguages
                    .Select(l => new LanguageInfo { Code = l.code.ToLowerInvariant(), Name = l.name })
                    .OrderBy(l => l.Code != _defaultLanguage)  // 默认语言排在最前面
                    .ThenBy(l => l.Name)
                    .ToList();
            }
            else
            {
                throw new Exception("No languages returned from API");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MultilingualInput] Failed to load languages: {ex.Message}");
            // 如果API调用失败，使用默认语言
            _languages = new List<LanguageInfo>
            {
                new() { Code = "ja", Name = "日本語" },
                new() { Code = "zh", Name = "中文" },
                new() { Code = "en", Name = "English" }
            };
        }

        // 初始化值字典
        if (_languages != null)
        {
            foreach (var lang in _languages)
            {
                _values[lang.Code] = Value?.GetValue(lang.Code);
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/multilingual-input.js");
            _dotNetRef = DotNetObjectReference.Create(this);
        }
    }

    private void HandleLanguageChanged()
    {
        // Update default language when system language changes
        if (DefaultLanguage == null) // Only update if not explicitly set
        {
            var newDefaultLang = I18n.CurrentLang.ToLowerInvariant();
            if (newDefaultLang != _defaultLanguage)
            {
                _defaultLanguage = newDefaultLang;

                // Re-order languages to put new default first
                if (_languages != null && _languages.Any())
                {
                    _languages = _languages
                        .OrderBy(l => l.Code != _defaultLanguage)
                        .ThenBy(l => l.Name)
                        .ToList();
                }

                InvokeAsync(StateHasChanged);
            }
        }
    }

    protected override void OnParametersSet()
    {
        if (Value != null && _languages != null)
        {
            foreach (var lang in _languages)
            {
                _values[lang.Code] = Value.GetValue(lang.Code);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Unsubscribe from language changes
        I18n.OnChanged -= HandleLanguageChanged;
        if (_jsModule != null)
        {
            await RemoveScrollListenerAsync();
            await _jsModule.DisposeAsync();
        }

        _dotNetRef?.Dispose();
    }

    private async Task OnValueChanged(string lang, string? value)
    {
        _values[lang] = value;
        await NotifyValueChanged();
    }

    private async Task NotifyValueChanged()
    {
        var newValue = new MultilingualTextDto();
        foreach (var kvp in _values)
        {
            if (!string.IsNullOrWhiteSpace(kvp.Value))
            {
                newValue[kvp.Key] = kvp.Value;
            }
        }

        if (ValueChanged.HasDelegate)
        {
            await ValueChanged.InvokeAsync(newValue);
        }
    }

    private string? GetDefaultValue()
    {
        // 先尝试获取默认语言的值
        if (_values.TryGetValue(_defaultLanguage, out var defaultValue) && !string.IsNullOrWhiteSpace(defaultValue))
        {
            return defaultValue;
        }

        // 如果默认语言为空,返回第一个非空值
        var firstNonEmpty = _values.FirstOrDefault(kvp => !string.IsNullOrWhiteSpace(kvp.Value));
        return firstNonEmpty.Value;
    }

    private string GetDefaultPlaceholder()
    {
        var langName = _languages?.FirstOrDefault(l => l.Code == _defaultLanguage)?.Name ?? _defaultLanguage.ToUpperInvariant();
        return $"{I18n.T("LBL_CLICK_TO_EDIT_MULTILINGUAL")} ({langName})";
    }

    private string GetPlaceholder(string langCode)
    {
        return langCode?.ToLowerInvariant() switch
        {
            "ja" => "日本語で入力してください",
            "zh" => "请输入中文",
            "en" => "Enter in English",
            "ko" => "한국어로 입력하세요",
            "fr" => "Entrez en français",
            "de" => "Auf Deutsch eingeben",
            "es" => "Ingrese en español",
            _ => "Enter text"
        };
    }

    private RenderFragment RenderOverlay() => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "multilingual-overlay");
        builder.AddAttribute(2, "id", _overlayId);

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
                builder.AddAttribute(7, "class", $"multilingual-input-row {(isDefault ? "default-language" : "")}");

                // Language label
                builder.OpenElement(8, "span");
                builder.AddAttribute(9, "class", "language-label");
                builder.AddAttribute(10, "title", isDefault ? I18n.T("LBL_DEFAULT_LANGUAGE") : "");
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

                // Input field
                builder.OpenComponent<Input<string>>(16);
                builder.AddAttribute(17, "Value", _values[lang.Code]);
                builder.AddAttribute(18, "ValueChanged", EventCallback.Factory.Create<string?>(this, v => OnValueChanged(lang.Code, v)));
                builder.AddAttribute(19, "Placeholder", GetPlaceholder(lang.Code));
                builder.AddAttribute(20, "Class", "field-control multilingual-field");
                builder.CloseComponent();

                builder.CloseElement(); // div.multilingual-input-row
            }
        }

        builder.CloseElement(); // div.multilingual-overlay
    };

    private async Task SetupScrollListenerAsync()
    {
        if (_jsModule == null || _dotNetRef == null)
        {
            return;
        }

        await _jsModule.InvokeVoidAsync("setupScrollListener", _dotNetRef);
    }

    private async Task RemoveScrollListenerAsync()
    {
        if (_jsModule == null)
        {
            return;
        }

        await _jsModule.InvokeVoidAsync("removeScrollListener");
    }

    private async Task<bool> IsFocusWithinOverlayAsync()
    {
        if (_jsModule == null)
        {
            return false;
        }

        return await _jsModule.InvokeAsync<bool>("isFocusWithinOverlay", _overlayId);
    }

    [JSInvokable]
    public Task CloseOnScroll()
    {
        if (_isExpanded)
        {
            _isExpanded = false;
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    private async Task HandleTriggerFocusIn(FocusEventArgs _)
    {
        _isExpanded = true;
        await SetupScrollListenerAsync();
        StateHasChanged();
    }

    private async Task HandleTriggerFocusOut(FocusEventArgs _)
    {
        await Task.Delay(120);
        if (!await IsFocusWithinOverlayAsync())
        {
            await ClosePopoverAsync();
        }
    }

    private async Task HandleKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            await ClosePopoverAsync();
        }
    }

    private async Task ClosePopoverAsync()
    {
        if (_isExpanded)
        {
            _isExpanded = false;
            await RemoveScrollListenerAsync();
            StateHasChanged();
        }
    }
}
