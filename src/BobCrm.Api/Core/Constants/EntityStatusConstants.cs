namespace BobCrm.Api.Core.Constants;

/// <summary>
/// 实体状态常量
/// </summary>
public static class EntityStatusConstants
{
    /// <summary>
    /// 草稿状态 - 实体定义尚未发布
    /// </summary>
    public const string Draft = "Draft";

    /// <summary>
    /// 已发布状态 - 实体定义已发布到数据库
    /// </summary>
    public const string Published = "Published";

    /// <summary>
    /// 已修改状态 - 已发布的实体被修改，等待重新发布
    /// </summary>
    public const string Modified = "Modified";

    /// <summary>
    /// 所有有效状态
    /// </summary>
    public static readonly string[] ValidStatuses = { Draft, Published, Modified };
}
