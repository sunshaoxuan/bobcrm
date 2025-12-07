namespace BobCrm.Api.Contracts.Responses.Entity;

/// <summary>
/// 模板绑定信息 DTO - 用于发布结果
/// </summary>
public class TemplateBindingInfoDto
{
    public string ViewState { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;
    public string UsageType { get; set; } = string.Empty;
    public int BindingId { get; set; }
    public int TemplateId { get; set; }
    public string? RequiredPermission { get; set; }
}
