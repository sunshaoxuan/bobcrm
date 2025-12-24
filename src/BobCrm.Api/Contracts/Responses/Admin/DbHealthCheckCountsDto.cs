namespace BobCrm.Api.Contracts.Responses.Admin;

/// <summary>
/// 数据库健康检查统计信息。
/// </summary>
public class DbHealthCheckCountsDto
{
    public int Customers { get; set; }
    public int FieldDefinitions { get; set; }
    public int FieldValues { get; set; }
    public int UserLayouts { get; set; }
}

