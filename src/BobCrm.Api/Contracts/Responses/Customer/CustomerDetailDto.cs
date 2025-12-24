namespace BobCrm.Api.Contracts.Responses.Customer;

/// <summary>
/// 客户详情（含字段值）。
/// </summary>
public class CustomerDetailDto
{
    /// <summary>
    /// 客户 Id。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 客户编码。
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 客户名称（已按语言解析）。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 乐观并发版本号。
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// 字段列表。
    /// </summary>
    public List<CustomerDetailFieldDto> Fields { get; set; } = new();
}

