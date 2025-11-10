namespace BobCrm.App.Services;

/// <summary>
/// 多语言文本提取辅助服务
/// 根据当前用户选择的语言动态提取 Dictionary 中的值
/// </summary>
public class MultilingualHelper
{
    private readonly I18nService _i18n;

    public MultilingualHelper(I18nService i18n)
    {
        _i18n = i18n;
    }

    /// <summary>
    /// 从多语言字典中获取当前语言的值
    /// </summary>
    /// <param name="multilingual">多语言字典</param>
    /// <param name="fallback">如果所有语言都为空，返回的默认值</param>
    /// <returns>当前语言的文本，如果不存在则返回回退值</returns>
    public string GetText(Dictionary<string, string?>? multilingual, string fallback = "")
    {
        if (multilingual == null || multilingual.Count == 0)
            return fallback;

        var currentLang = _i18n.CurrentLang?.ToLowerInvariant() ?? "ja";

        // 1. 尝试当前用户选择的语言
        if (multilingual.TryGetValue(currentLang, out var value) && !string.IsNullOrWhiteSpace(value))
            return value;

        // 2. 尝试默认语言（如果当前语言不是默认语言）
        if (currentLang != "ja" && multilingual.TryGetValue("ja", out var jaValue) && !string.IsNullOrWhiteSpace(jaValue))
            return jaValue;

        // 3. 尝试英语
        if (multilingual.TryGetValue("en", out var enValue) && !string.IsNullOrWhiteSpace(enValue))
            return enValue;

        // 4. 尝试中文
        if (multilingual.TryGetValue("zh", out var zhValue) && !string.IsNullOrWhiteSpace(zhValue))
            return zhValue;

        // 5. 返回第一个非空值
        var firstNonEmpty = multilingual.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
        if (!string.IsNullOrWhiteSpace(firstNonEmpty))
            return firstNonEmpty;

        // 6. 全部为空，返回 fallback
        return fallback;
    }

    /// <summary>
    /// 获取当前用户选择的语言代码
    /// </summary>
    public string CurrentLanguage => _i18n.CurrentLang?.ToLowerInvariant() ?? "ja";
}
