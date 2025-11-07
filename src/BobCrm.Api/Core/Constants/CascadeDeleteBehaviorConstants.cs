namespace BobCrm.Api.Core.Constants;

/// <summary>
/// 级联删除行为常量
/// </summary>
public static class CascadeDeleteBehaviorConstants
{
    /// <summary>
    /// 无操作 - 删除主记录时不影响子记录（需手动处理）
    /// </summary>
    public const string NoAction = "NoAction";

    /// <summary>
    /// 级联删除 - 删除主记录时自动删除所有子记录
    /// </summary>
    public const string Cascade = "Cascade";

    /// <summary>
    /// 设为空 - 删除主记录时将子记录的外键设为NULL
    /// </summary>
    public const string SetNull = "SetNull";

    /// <summary>
    /// 限制删除 - 存在子记录时禁止删除主记录
    /// </summary>
    public const string Restrict = "Restrict";

    /// <summary>
    /// 所有有效的级联删除行为
    /// </summary>
    public static readonly string[] ValidBehaviors =
    {
        NoAction, Cascade, SetNull, Restrict
    };
}
