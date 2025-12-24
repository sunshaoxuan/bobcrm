namespace BobCrm.Api.Contracts.Responses.Template;

/// <summary>
/// 模板查询响应（支持平铺或分组）。
/// </summary>
public class TemplateQueryResponseDto
{
    /// <summary>
    /// 分组方式：none/entity/user。
    /// </summary>
    public string GroupBy { get; set; } = "none";

    /// <summary>
    /// 平铺模板列表（GroupBy=none 时使用）。
    /// </summary>
    public List<TemplateSummaryDto> Items { get; set; } = new();

    /// <summary>
    /// 按实体分组（GroupBy=entity 时使用）。
    /// </summary>
    public List<TemplateGroupByEntityDto> GroupsByEntity { get; set; } = new();

    /// <summary>
    /// 按用户分组（GroupBy=user 时使用）。
    /// </summary>
    public List<TemplateGroupByUserDto> GroupsByUser { get; set; } = new();
}

