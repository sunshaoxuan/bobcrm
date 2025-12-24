namespace BobCrm.Api.Contracts.Responses.Admin;

/// <summary>
/// 重新生成默认模板结果（开发环境）。
/// </summary>
public class RegenerateDefaultTemplatesResultDto
{
    public int Entities { get; set; }
    public int Updated { get; set; }
    public string Message { get; set; } = string.Empty;
}

