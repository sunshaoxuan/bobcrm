namespace BobCrm.Api.Contracts.Responses.Permissions;

/// <summary>
/// 字段权限检查结果。
/// </summary>
public class FieldPermissionCheckDto
{
    /// <summary>
    /// 是否允许。
    /// </summary>
    public bool Allowed { get; set; }
}

