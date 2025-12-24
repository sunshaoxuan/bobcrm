namespace BobCrm.Api.Base.Models;

/// <summary>
/// 实体来源枚举
/// </summary>
public static class EntitySource
{
    /// <summary>系统初始化实体（从代码同步）</summary>
    public const string System = "System";

    /// <summary>用户自定义实体（通过UI创建）</summary>
    public const string Custom = "Custom";
}

