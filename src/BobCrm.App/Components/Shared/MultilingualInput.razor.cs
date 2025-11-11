using BobCrm.App.Models;
using BobCrm.App.Services;
using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;

namespace BobCrm.App.Components.Shared;

public partial class MultilingualInput
{
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private I18nService I18n { get; set; } = default!;

    [Parameter] public MultilingualTextDto? Value { get; set; }
    [Parameter] public EventCallback<MultilingualTextDto?> ValueChanged { get; set; }
    [Parameter] public string? DefaultLanguage { get; set; }

    private readonly Trigger[] _trigger = [Trigger.Click];
    private List<LanguageInfo>? _languages;
    private Dictionary<string, string?> _values = new();
    private bool _isExpanded = false;
    private string _defaultLanguage = "ja";

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

    private void HandleVisibleChange(bool visible)
    {
        _isExpanded = visible;
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

    private RenderFragment GetSuffixTemplate() => @<text>
        <span style="display: flex; align-items: center; gap: 4px; height: 100%;">
            <Icon Type="global" Style="color: #1890ff;" />
            <Icon Type="@(_isExpanded ? "up" : "down")" Style="font-size: 10px;" />
        </span>
    </text>;

    private RenderFragment RenderOverlay() => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "multilingual-overlay");

        if (_languages == null || !_languages.Any())
        {
            builder.OpenElement(2, "div");
            builder.AddAttribute(3, "class", "multilingual-loading");
            builder.OpenComponent<AntDesign.Spin>(4);
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
                    builder.OpenComponent<AntDesign.Icon>(12);
                    builder.AddAttribute(13, "Type", "star");
                    builder.AddAttribute(14, "Theme", "filled");
                    builder.AddAttribute(15, "Style", "font-size: 10px; color: #faad14; margin-left: 4px;");
                    builder.CloseComponent();
                }

                builder.CloseElement(); // span

                // Input field
                builder.OpenComponent<AntDesign.Input>(16);
                builder.AddAttribute(17, "Value", _values[lang.Code]);
                builder.AddAttribute(18, "ValueChanged", EventCallback.Factory.Create<string?>(this, v => OnValueChanged(lang.Code, v)));
                builder.AddAttribute(19, "Placeholder", GetPlaceholder(lang.Code));
                builder.AddAttribute(20, "Class", "field-control multilingual-field");
                builder.AddAttribute(21, "AllowClear", true);
                builder.CloseComponent();

                builder.CloseElement(); // div.multilingual-input-row
            }
        }

        builder.CloseElement(); // div.multilingual-overlay
    };
}
