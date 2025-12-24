namespace BobCrm.Api.Contracts.Responses.Customer;

/// <summary>
/// 创建客户结果。
/// </summary>
public class CustomerCreateResultDto
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
    /// 客户名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

