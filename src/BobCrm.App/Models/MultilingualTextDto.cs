namespace BobCrm.App.Models;

/// <summary>
/// 多语言文本DTO - 用于元数据多语言输入
/// </summary>
public class MultilingualTextDto
{
    /// <summary>
    /// 中文文本
    /// </summary>
    public string? ZH { get; set; }

    /// <summary>
    /// 日语文本
    /// </summary>
    public string? JA { get; set; }

    /// <summary>
    /// 英文文本
    /// </summary>
    public string? EN { get; set; }

    /// <summary>
    /// 检查是否有任何非空值
    /// </summary>
    public bool HasValue() => !string.IsNullOrWhiteSpace(ZH)
                            || !string.IsNullOrWhiteSpace(JA)
                            || !string.IsNullOrWhiteSpace(EN);

    /// <summary>
    /// 获取指定语言的值
    /// </summary>
    public string? GetValue(string lang)
    {
        return lang?.ToLowerInvariant() switch
        {
            "zh" => ZH,
            "ja" => JA,
            "en" => EN,
            _ => JA // 默认返回日语
        };
    }
}
