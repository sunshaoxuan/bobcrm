namespace BobCrm.Api.Contracts.Responses.Layout;

/// <summary>
/// 字段动作（用于 UI 渲染）。
/// </summary>
public class FieldActionResponseDto
{
    /// <summary>
    /// 图标（可选）。
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 动作标题（已按语言解析，可选）。
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 动作标题的 i18n Key（可选）。
    /// </summary>
    public string? TitleKey { get; set; }

    /// <summary>
    /// 动作类型（可选）。
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 动作标识（可选）。
    /// </summary>
    public string? Action { get; set; }
}

