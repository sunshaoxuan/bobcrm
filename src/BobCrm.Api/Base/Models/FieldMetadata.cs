using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BobCrm.Api.Base.Models;

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
    /// 所属子实体ID（可选）
    /// 如果为空，表示是主实体字段；否则表示是子实体字段
    /// </summary>
    public Guid? SubEntityDefinitionId { get; set; }

    /// <summary>
    /// 属性名（代码中的名称，如 Email）
    /// </summary>
    [Required, MaxLength(100)]
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// 显示名资源 Key（用于接口字段）
    /// 示例：LBL_FIELD_CODE, LBL_FIELD_CREATED_AT
    /// </summary>
    [MaxLength(100)]
    public string? DisplayNameKey { get; set; }

    /// <summary>
    /// 显示名（多语言）- PostgreSQL jsonb 类型
    /// 示例：{"ja": "価格", "zh": "价格", "en": "Price"}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, string?>? DisplayName { get; set; }

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
    private bool _isRequired = true;
    private bool _isRequiredSet;

    public bool IsRequired
    {
        get => _isRequired;
        set
        {
            _isRequired = value;
            _isRequiredSet = true;
        }
    }

    [NotMapped]
    public bool IsRequiredExplicitlySet => _isRequiredSet;

    /// <summary>
    /// 是否为聚合子实体引用
    /// </summary>
    public bool IsEntityRef { get; set; } = false;

    /// <summary>
    /// 引用的实体定义ID（IsEntityRef=true时有效）
    /// </summary>
    public Guid? ReferencedEntityId { get; set; }

    [MaxLength(100)]
    public string? LookupEntityName { get; set; }

    [MaxLength(100)]
    public string? LookupDisplayField { get; set; }

    public ForeignKeyAction ForeignKeyAction { get; set; } = ForeignKeyAction.Restrict;

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
    /// 字段来源（System/Custom/Interface）
    /// System: 系统实体的原始字段（不可删除/编辑）
    /// Custom: 用户自定义字段（可删除/编辑）
    /// Interface: 接口字段（Base、Archive、Audit等）
    /// </summary>
    [MaxLength(20)]
    public string? Source { get; set; }

    /// <summary>
    /// 是否已删除（软删除标记）
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// 删除时间
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// 删除者
    /// </summary>
    [MaxLength(100)]
    public string? DeletedBy { get; set; }

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

    // ========== Enum Support (Dynamic Enum System) ==========
    
    /// <summary>
    /// 引用的枚举定义ID
    /// 当 DataType = "Enum" 时必填
    /// </summary>
    public Guid? EnumDefinitionId { get; set; }
    
    /// <summary>
    /// 是否允许多选（枚举字段专用）
    /// true: 存储为JSON数组 ["VAL1", "VAL2"]
    /// false: 存储为单个值 "VAL1"
    /// </summary>
    public bool IsMultiSelect { get; set; } = false;
    
    /// <summary>
    /// 引用的枚举定义（导航属性）
    /// </summary>
    public EnumDefinition? EnumDefinition { get; set; }

    /// <summary>
    /// 所属子实体（导航属性）
    /// </summary>
    public SubEntityDefinition? SubEntityDefinition { get; set; }
}
