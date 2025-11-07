using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Domain.Models;

/// <summary>
/// 字段元数据 - 实体字段的定义
/// </summary>
public class FieldMetadata
{
    /// <summary>
    /// 字段元数据ID
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 所属实体定义ID
    /// </summary>
    [Required]
    public Guid EntityDefinitionId { get; set; }

    /// <summary>
    /// 父字段ID（用于子实体场景，指向主实体的字段）
    /// </summary>
    public Guid? ParentFieldId { get; set; }

    /// <summary>
    /// 属性名（代码中的名称，如 Email）
    /// </summary>
    [Required, MaxLength(100)]
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// 显示名多语言键
    /// </summary>
    [Required, MaxLength(100)]
    public string DisplayNameKey { get; set; } = string.Empty;

    /// <summary>
    /// 数据类型（String、Int32、DateTime、Decimal、Boolean、Guid、EntityRef）
    /// </summary>
    [Required, MaxLength(50)]
    public string DataType { get; set; } = "String";

    /// <summary>
    /// 数据长度（字符串类型时有效）
    /// </summary>
    public int? Length { get; set; }

    /// <summary>
    /// 小数精度（Decimal类型时有效）
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// 小数刻度（Decimal类型时有效）
    /// </summary>
    public int? Scale { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// 是否为聚合子实体引用
    /// </summary>
    public bool IsEntityRef { get; set; } = false;

    /// <summary>
    /// 引用的实体定义ID（IsEntityRef=true时有效）
    /// </summary>
    public Guid? ReferencedEntityId { get; set; }

    /// <summary>
    /// 物理表名（发布后填充，发布后不可修改）
    /// </summary>
    [MaxLength(100)]
    public string? TableName { get; set; }

    /// <summary>
    /// 排序顺序
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 默认值
    /// </summary>
    [MaxLength(500)]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 验证规则（JSON格式）
    /// </summary>
    public string? ValidationRules { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 所属实体定义（导航属性）
    /// </summary>
    public EntityDefinition? EntityDefinition { get; set; }

    /// <summary>
    /// 父字段（导航属性）
    /// </summary>
    public FieldMetadata? ParentField { get; set; }

    /// <summary>
    /// 子字段集合（导航属性）
    /// </summary>
    public List<FieldMetadata> ChildFields { get; set; } = new();

    /// <summary>
    /// 引用的实体定义（导航属性）
    /// </summary>
    public EntityDefinition? ReferencedEntity { get; set; }
}

/// <summary>
/// 数据类型枚举
/// </summary>
public static class FieldDataType
{
    public const string String = "String";
    public const string Int32 = "Int32";
    public const string Int64 = "Int64";
    public const string Decimal = "Decimal";
    public const string DateTime = "DateTime";
    public const string Boolean = "Boolean";
    public const string Guid = "Guid";
    public const string EntityRef = "EntityRef";  // 子实体引用
}
