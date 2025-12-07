namespace BobCrm.Api.Contracts.Responses.Entity;

/// <summary>
/// 模板信息 DTO - 用于发布结果
/// </summary>
public class TemplateInfoDto
{
    public string ViewState { get; set; } = string.Empty;
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
}
