namespace BobCrm.Api.Contracts.Responses.Admin;

/// <summary>
/// 全量重置所有实体模板结果（开发/测试环境）。
/// </summary>
public class ResetAllTemplatesResultDto
{
    public List<string> Entities { get; set; } = new();
    public int DeletedTemplates { get; set; }
    public int DeletedStateBindings { get; set; }
    public int DeletedLegacyBindings { get; set; }
    public int Created { get; set; }
    public int Updated { get; set; }
}

