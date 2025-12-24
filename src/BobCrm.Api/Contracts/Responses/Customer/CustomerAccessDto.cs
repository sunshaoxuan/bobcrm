namespace BobCrm.Api.Contracts.Responses.Customer;

/// <summary>
/// 客户访问权限条目。
/// </summary>
public class CustomerAccessDto
{
    /// <summary>
    /// 用户 Id。
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 是否可编辑。
    /// </summary>
    public bool CanEdit { get; set; }
}

