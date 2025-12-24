namespace BobCrm.Api.Contracts.Responses.Template;

/// <summary>
/// 菜单与模板交集条目。
/// </summary>
public class MenuTemplateIntersectionDto
{
    /// <summary>
    /// 菜单节点信息。
    /// </summary>
    public MenuNodeSummaryDto Menu { get; set; } = new();

    /// <summary>
    /// 模板绑定信息。
    /// </summary>
    public TemplateStateBindingSummaryDto Binding { get; set; } = new();

    /// <summary>
    /// 可选模板列表。
    /// </summary>
    public List<TemplateSummaryDto> Templates { get; set; } = new();
}

