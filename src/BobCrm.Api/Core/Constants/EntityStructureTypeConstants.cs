namespace BobCrm.Api.Core.Constants;

/// <summary>
/// 实体结构类型常量
/// </summary>
public static class EntityStructureTypeConstants
{
    /// <summary>
    /// 单一实体 - 独立实体，无主子关系
    /// </summary>
    public const string Single = "Single";

    /// <summary>
    /// 主子结构（两层）- 一对多关系，主实体+子实体
    /// </summary>
    public const string MasterDetail = "MasterDetail";

    /// <summary>
    /// 主子孙结构（三层）- 两层一对多关系，主实体+子实体+孙实体
    /// </summary>
    public const string MasterDetailGrandchild = "MasterDetailGrandchild";

    /// <summary>
    /// 所有有效的结构类型
    /// </summary>
    public static readonly string[] ValidStructureTypes =
    {
        Single, MasterDetail, MasterDetailGrandchild
    };
}
