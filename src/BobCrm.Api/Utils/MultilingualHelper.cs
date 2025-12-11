namespace BobCrm.Api.Utils;

/// <summary>
/// 多语字典解析工具类
/// </summary>
public static class MultilingualHelper
{
    /// <summary>
    /// 从多语字典中解析指定语言的文本
    /// </summary>
    /// <param name="dict">多语字典 (key: 语言代码, value: 翻译文本)</param>
    /// <param name="lang">目标语言代码 (zh/ja/en)</param>
    /// <returns>解析后的文本，如果目标语言不存在则返回第一个非空值</returns>
    public static string Resolve(this Dictionary<string, string?>? dict, string lang)
    {
        if (dict == null || dict.Count == 0)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(lang) &&
            dict.TryGetValue(lang, out var value) &&
            !string.IsNullOrWhiteSpace(value))
        {
            return value!;
        }

        return dict.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
    }

    /// <summary>
    /// 批量解析多语字典
    /// </summary>
    /// <param name="dicts">多个多语字典</param>
    /// <param name="lang">目标语言代码</param>
    /// <returns>键值对，值为解析后的单语文本</returns>
    public static Dictionary<string, string> ResolveBatch(
        this Dictionary<string, Dictionary<string, string?>?>? dicts,
        string lang)
    {
        if (dicts == null || dicts.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        return dicts.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Resolve(lang));
    }
}
