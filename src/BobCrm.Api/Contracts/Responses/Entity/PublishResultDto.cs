namespace BobCrm.Api.Contracts.Responses.Entity;

/// <summary>
/// 实体发布结果 DTO
/// </summary>
public class PublishResultDto
{
    public bool Success { get; set; }
    public Guid? ScriptId { get; set; }
    public string? DdlScript { get; set; }
    public ChangeAnalysisDto? ChangeAnalysis { get; set; }
    public IEnumerable<TemplateInfoDto> Templates { get; set; } = Enumerable.Empty<TemplateInfoDto>();
    public IEnumerable<TemplateBindingInfoDto> Bindings { get; set; } = Enumerable.Empty<TemplateBindingInfoDto>();
    public IEnumerable<MenuNodeInfoDto> Menus { get; set; } = Enumerable.Empty<MenuNodeInfoDto>();
    public string Message { get; set; } = string.Empty;
}
