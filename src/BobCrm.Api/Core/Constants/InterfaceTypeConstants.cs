namespace BobCrm.Api.Core.Constants;

/// <summary>
/// 实体接口类型常量
/// </summary>
public static class InterfaceTypeConstants
{
    /// <summary>
    /// 基础接口 - 包含Id、Code、Name等基础字段
    /// </summary>
    public const string Base = "Base";

    /// <summary>
    /// 归档接口 - 包含IsArchived、ArchivedAt、ArchivedBy等归档字段
    /// </summary>
    public const string Archive = "Archive";

    /// <summary>
    /// 审计接口 - 包含CreatedAt、CreatedBy、UpdatedAt、UpdatedBy等审计字段
    /// </summary>
    public const string Audit = "Audit";

    /// <summary>
    /// 树形接口 - 包含ParentId、Level、Path等树形结构字段
    /// </summary>
    public const string Tree = "Tree";

    /// <summary>
    /// 软删除接口 - 包含IsDeleted、DeletedAt、DeletedBy等软删除字段
    /// </summary>
    public const string SoftDelete = "SoftDelete";

    /// <summary>
    /// 所有有效的接口类型
    /// </summary>
    public static readonly string[] ValidInterfaceTypes =
    {
        Base, Archive, Audit, Tree, SoftDelete
    };
}
