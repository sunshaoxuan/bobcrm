namespace BobCrm.App.Models;

/// <summary>
/// 多语言文本DTO - 用于元数据多语言输入
/// 使用动态结构，支持任意语言
/// </summary>
public class MultilingualTextDto : Dictionary<string, string?>
{
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public MultilingualTextDto() : base(StringComparer.OrdinalIgnoreCase)
    {
    }

    /// <summary>
    /// 构造函数 - 从 Dictionary&lt;string, string?&gt; 创建（API 响应格式）
    /// </summary>
    public MultilingualTextDto(Dictionary<string, string?>? source) : base(StringComparer.OrdinalIgnoreCase)
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
    /// 构造函数 - 从泛型集合创建
    /// </summary>
    public MultilingualTextDto(IEnumerable<KeyValuePair<string, string?>>? source) : base(StringComparer.OrdinalIgnoreCase)
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
