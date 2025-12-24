using BobCrm.Api.Base;

namespace BobCrm.Api.Contracts.Responses.Admin;

/// <summary>
/// 重置指定实体的系统模板与绑定结果（开发环境）。
/// </summary>
public class ResetTemplatesForEntityResultDto
{
    public string Entity { get; set; } = string.Empty;
    public int Deleted { get; set; }
    public int DeletedBindings { get; set; }
    public int CreatedSystemTemplates { get; set; }
    public List<string> CreatedTemplates { get; set; } = new();
    public List<FormTemplate> CurrentTemplates { get; set; } = new();
}
