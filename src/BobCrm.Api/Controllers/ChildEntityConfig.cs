namespace BobCrm.Api.Controllers;

/// <summary>
/// 子实体配置
/// </summary>
public class ChildEntityConfig
{
    /// <summary>子实体ID</summary>
    public Guid ChildEntityId { get; set; }

    /// <summary>外键字段名</summary>
    public string ForeignKeyField { get; set; } = string.Empty;

    /// <summary>集合属性名</summary>
    public string CollectionProperty { get; set; } = string.Empty;

    /// <summary>级联删除行为</summary>
    public string? CascadeDeleteBehavior { get; set; }

    /// <summary>自动级联保存</summary>
    public bool? AutoCascadeSave { get; set; }
}
