namespace BobCrm.Api.Contracts.Responses.Template;

/// <summary>
/// 模板按用户分组结果。
/// </summary>
public class TemplateGroupByUserDto
{
    /// <summary>
    /// 用户 Id。
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 模板列表。
    /// </summary>
    public List<TemplateSummaryDto> Templates { get; set; } = new();
}

