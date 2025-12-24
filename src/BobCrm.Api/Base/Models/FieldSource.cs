namespace BobCrm.Api.Base.Models;

/// <summary>
/// 字段来源枚举
/// </summary>
public static class FieldSource
{
    /// <summary>
    /// 系统字段 - 系统实体的原始字段，不可删除/编辑
    /// </summary>
    public const string System = "System";

    /// <summary>
    /// 自定义字段 - 用户添加的字段，可删除/编辑
    /// </summary>
    public const string Custom = "Custom";

    /// <summary>
    /// 接口字段 - 来自接口的字段（Base、Archive、Audit等），由接口定义管理
    /// </summary>
    public const string Interface = "Interface";
}

