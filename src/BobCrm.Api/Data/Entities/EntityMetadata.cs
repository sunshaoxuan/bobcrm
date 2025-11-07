namespace BobCrm.Api.Data.Entities;

/// <summary>
/// 实体元数据表 - 存储可用于创建模板的根实体信息
/// 只有根实体（非聚合子实体）才能创建独立的模板
/// </summary>
public class EntityMetadata
{
    /// <summary>实体类型（主键）- 类的全名（如：BobCrm.Api.Domain.Customer）</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>实体名称 - 类的本名（如：Customer，保持原始大小写）</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>实体URL路径名 - 用于路由（如：customer，小写）</summary>
    public string EntityRoute { get; set; } = string.Empty;

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

    /// <summary>实体来源：System（系统内置硬编码实体）或 Custom（用户自定义实体）</summary>
    public string EntitySource { get; set; } = "System";

    /// <summary>如果是自定义实体，指向 EntityDefinition.Id</summary>
    public Guid? SourceDefinitionId { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>最后更新时间</summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 实体来源枚举
/// </summary>
public static class EntitySource
{
    /// <summary>系统内置实体（硬编码）</summary>
    public const string System = "System";

    /// <summary>用户自定义实体（通过EntityDefinition创建）</summary>
    public const string Custom = "Custom";
}

