namespace BobCrm.Api.Contracts.Responses.Template;

/// <summary>
/// 模板按实体分组结果。
/// </summary>
public class TemplateGroupByEntityDto
{
    /// <summary>
    /// 实体类型分组键。
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// 模板列表。
    /// </summary>
    public List<TemplateSummaryDto> Templates { get; set; } = new();
}

