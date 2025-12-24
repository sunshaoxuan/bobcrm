namespace BobCrm.Api.Contracts.Responses.Admin;

/// <summary>
/// 为指定实体重新生成默认模板结果（开发环境）。
/// </summary>
public class RegenerateDefaultTemplatesForEntityResultDto
{
    public string Entity { get; set; } = string.Empty;
    public int Created { get; set; }
    public int Updated { get; set; }
}

