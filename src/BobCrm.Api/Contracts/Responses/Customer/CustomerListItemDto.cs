namespace BobCrm.Api.Contracts.Responses.Customer;

/// <summary>
/// 客户列表条目。
/// </summary>
public class CustomerListItemDto
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
}

