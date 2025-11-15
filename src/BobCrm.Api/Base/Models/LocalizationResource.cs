using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Base;

/// <summary>
/// 国际化资源 - 使用 Dictionary 动态支持任意语言
/// </summary>
public class LocalizationResource
{
    /// <summary>
    /// 资源键（唯一标识符）
    /// </summary>
    [Key, MaxLength(256)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 翻译字典：{ "zh": "客户", "ja": "顧客", "en": "Customer" }
    /// 使用 Npgsql 的 jsonb 类型存储，支持动态语言扩展
    /// </summary>
    public Dictionary<string, string> Translations { get; set; } = new();
}

