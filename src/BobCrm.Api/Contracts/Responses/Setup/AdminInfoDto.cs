namespace BobCrm.Api.Contracts.Responses.Setup;

/// <summary>
/// 系统初始化阶段管理员信息。
/// </summary>
public class AdminInfoDto
{
    /// <summary>
    /// 用户名（可选）。
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱（可选）。
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 是否已存在管理员账户。
    /// </summary>
    public bool Exists { get; set; }
}

