namespace BobCrm.Api.Data.Entities;

/// <summary>
/// 实体元数据表 - 存储可用于创建模板的根实体信息
/// 只有根实体（非聚合子实体）才能创建独立的模板
/// </summary>
public class EntityMetadata
{
    /// <summary>实体类型标识（主键，如：customer, product）</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>实体显示名称的多语键（如：ENTITY_CUSTOMER）</summary>
    public string DisplayNameKey { get; set; } = string.Empty;

    /// <summary>实体描述的多语键（可选）</summary>
    public string? DescriptionKey { get; set; }

    /// <summary>API端点基础路径（如：/api/customers）</summary>
    public string ApiEndpoint { get; set; } = string.Empty;

    /// <summary>是否为根实体（只有根实体可创建模板）</summary>
    public bool IsRootEntity { get; set; } = true;

    /// <summary>是否启用（可用于临时禁用某些实体）</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>排序顺序</summary>
    public int Order { get; set; }

    /// <summary>图标（可选，用于UI显示）</summary>
    public string? Icon { get; set; }

    /// <summary>实体分类（如：core, sales, service）</summary>
    public string? Category { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>最后更新时间</summary>
    public DateTime? UpdatedAt { get; set; }
}

