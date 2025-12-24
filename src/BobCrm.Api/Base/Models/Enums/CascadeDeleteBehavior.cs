namespace BobCrm.Api.Base.Models;

/// <summary>
/// 级联删除行为枚举
/// </summary>
public static class CascadeDeleteBehavior
{
    /// <summary>不执行任何操作</summary>
    public const string NoAction = "NoAction";

    /// <summary>级联删除相关记录</summary>
    public const string Cascade = "Cascade";

    /// <summary>将外键设置为NULL</summary>
    public const string SetNull = "SetNull";

    /// <summary>阻止删除（抛出错误）</summary>
    public const string Restrict = "Restrict";
}

