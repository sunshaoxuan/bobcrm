namespace BobCrm.Api.Contracts.Responses.Template;

/// <summary>
/// 应用模板结果。
/// </summary>
public class ApplyTemplateResultDto
{
    /// <summary>
    /// 提示消息。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 应用后的模板信息。
    /// </summary>
    public AppliedTemplateDto Template { get; set; } = new();
}

