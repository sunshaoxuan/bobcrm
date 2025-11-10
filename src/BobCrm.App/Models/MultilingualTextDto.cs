namespace BobCrm.App.Models;

/// <summary>
/// 多语言文本DTO - 用于元数据多语言输入
/// 使用动态结构，支持任意语言
/// </summary>
public class MultilingualTextDto : Dictionary<string, string?>
{
    public MultilingualTextDto() : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    public MultilingualTextDto(Dictionary<string, string> source) : base(StringComparer.OrdinalIgnoreCase)
    {
        if (source != null)
        {
            foreach (var kvp in source)
            {
                this[kvp.Key] = kvp.Value;
            }
        }
    }

    /// <summary>
    /// 检查是否有任何非空值
    /// </summary>
    public bool HasValue() => this.Values.Any(v => !string.IsNullOrWhiteSpace(v));

    /// <summary>
    /// 获取指定语言的值
    /// </summary>
    public string? GetValue(string lang)
    {
        return TryGetValue(lang?.ToLowerInvariant() ?? "ja", out var value) ? value : null;
    }

    /// <summary>
    /// 设置指定语言的值
    /// </summary>
    public void SetValue(string lang, string? value)
    {
        this[lang?.ToLowerInvariant() ?? "ja"] = value;
    }
}
