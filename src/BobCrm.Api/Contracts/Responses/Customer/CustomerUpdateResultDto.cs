namespace BobCrm.Api.Contracts.Responses.Customer;

/// <summary>
/// 更新客户结果。
/// </summary>
public class CustomerUpdateResultDto
{
    /// <summary>
    /// 更新状态。
    /// </summary>
    public string Status { get; set; } = "success";

    /// <summary>
    /// 更新后的版本号。
    /// </summary>
    public int NewVersion { get; set; }
}

