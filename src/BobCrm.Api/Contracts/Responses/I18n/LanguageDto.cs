namespace BobCrm.Api.Contracts.Responses.I18n;

/// <summary>
/// 可用语言条目。
/// </summary>
public class LanguageDto
{
    /// <summary>
    /// 语言代码（如 en/zh/ja）。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 语言名称（NativeName）。
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

