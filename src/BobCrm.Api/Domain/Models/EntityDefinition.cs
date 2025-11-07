using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Domain.Models;

/// <summary>
/// 实体定义 - 用于自定义实体的元数据
/// </summary>
public class EntityDefinition
{
    /// <summary>
    /// 实体定义ID
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 命名空间（如 BobCrm.Domain.Custom）
    /// </summary>
    [Required, MaxLength(500)]
    public string Namespace { get; set; } = "BobCrm.Domain.Custom";

    /// <summary>
    /// 实体名（如 Product）
    /// </summary>
    [Required, MaxLength(100)]
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// 显示名多语言键
    /// </summary>
    [Required, MaxLength(100)]
    public string DisplayNameKey { get; set; } = string.Empty;

    /// <summary>
    /// 描述多语言键
    /// </summary>
    [MaxLength(100)]
    public string? DescriptionKey { get; set; }

    /// <summary>
    /// 结构类型：Single（单实体）、MasterDetail（主子）、MasterDetailGrandchild（主子孙）
    /// </summary>
    [Required, MaxLength(50)]
    public string StructureType { get; set; } = "Single";

    /// <summary>
    /// 状态：Draft（草稿）、Published（已发布）、Modified（已修改）
    /// </summary>
    [Required, MaxLength(50)]
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// 是否已被模板引用锁定（锁定后实体类型和名称不可修改）
    /// </summary>
    public bool IsLocked { get; set; } = false;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 创建人
    /// </summary>
    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 更新人
    /// </summary>
    [MaxLength(100)]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// 字段元数据集合（导航属性）
    /// </summary>
    public List<FieldMetadata> Fields { get; set; } = new();

    /// <summary>
    /// 实体接口集合（导航属性）
    /// </summary>
    public List<EntityInterface> Interfaces { get; set; } = new();

    /// <summary>
    /// DDL脚本集合（导航属性）
    /// </summary>
    public List<DDLScript> DDLScripts { get; set; } = new();

    /// <summary>
    /// 获取实体全名（命名空间.实体名）
    /// </summary>
    public string FullName => $"{Namespace}.{EntityName}";

    /// <summary>
    /// 获取默认表名（实体名复数形式）
    /// </summary>
    public string DefaultTableName => EntityName + "s";
}

/// <summary>
/// 实体结构类型枚举
/// </summary>
public static class EntityStructureType
{
    public const string Single = "Single";
    public const string MasterDetail = "MasterDetail";
    public const string MasterDetailGrandchild = "MasterDetailGrandchild";
}

/// <summary>
/// 实体状态枚举
/// </summary>
public static class EntityStatus
{
    public const string Draft = "Draft";
    public const string Published = "Published";
    public const string Modified = "Modified";
}
