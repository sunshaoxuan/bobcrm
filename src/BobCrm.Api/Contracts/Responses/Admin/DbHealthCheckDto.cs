namespace BobCrm.Api.Contracts.Responses.Admin;

/// <summary>
/// 数据库健康检查结果（开发环境）。
/// </summary>
public class DbHealthCheckDto
{
    public string Provider { get; set; } = string.Empty;
    public bool CanConnect { get; set; }
    public DbHealthCheckCountsDto Counts { get; set; } = new();
}

