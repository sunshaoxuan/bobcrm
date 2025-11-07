namespace BobCrm.Api.Domain;

/// <summary>
/// 表单模板 - 支持每个实体多个命名模板
/// 每个用户可以有一个默认模板，系统也可以有一个默认模板
/// </summary>
public class FormTemplate
{
    /// <summary>模板ID（主键）</summary>
    public int Id { get; set; }

    /// <summary>模板名称（用户可见）</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 实体类型（如 "customer", "product", "order"）
    /// 一旦设置后不允许修改
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>模板创建者的用户ID</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 是否为用户默认模板
    /// 每个用户在每个实体类型下只能有一个默认模板
    /// </summary>
    public bool IsUserDefault { get; set; } = false;

    /// <summary>
    /// 是否为系统默认模板
    /// 每个实体类型只能有一个系统默认模板
    /// 系统默认模板用于新用户或没有用户默认模板的用户
    /// </summary>
    public bool IsSystemDefault { get; set; } = false;

    /// <summary>布局JSON（Widget树）</summary>
    public string? LayoutJson { get; set; }

    /// <summary>模板描述（可选）</summary>
    public string? Description { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>最后更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 是否正在被使用
    /// 用于判断模板是否可以删除
    /// </summary>
    public bool IsInUse { get; set; } = false;
}
