using BobCrm.Api.Base;

namespace BobCrm.Api.Services;

/// <summary>
/// 发布结果
/// </summary>
public class PublishResult
{
    public Guid EntityDefinitionId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DDLScript { get; set; }
    public Guid ScriptId { get; set; }
    public ChangeAnalysis? ChangeAnalysis { get; set; }
    public List<PublishedTemplateInfo> Templates { get; } = new();
    public List<PublishedTemplateBindingInfo> TemplateBindings { get; } = new();
    public List<PublishedMenuInfo> MenuNodes { get; } = new();
}

