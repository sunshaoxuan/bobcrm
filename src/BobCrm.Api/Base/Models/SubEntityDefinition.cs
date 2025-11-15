using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BobCrm.Api.Base.Models;

/// <summary>
/// 子实体定义
/// 表示实体的1:N子表结构（如订单明细、评论列表等）
/// </summary>
public class SubEntityDefinition
{
    /// <summary>
    /// 子实体ID（主键）
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 所属主实体ID（外键）
    /// </summary>
    [Required]
    public Guid EntityDefinitionId { get; set; }

    /// <summary>
    /// 子实体编码（如 "Lines", "Items", "Comments"）
    /// 用于生成C#类名和表名
    /// 必须以大写字母开头，只能包含字母和数字
    /// </summary>
    [Required, MaxLength(100)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称（多语言）- PostgreSQL jsonb 类型
    /// 示例：{"ja": "明細", "zh": "明细", "en": "Lines"}
    /// </summary>
    [Required]
    [Column(TypeName = "jsonb")]
    public Dictionary<string, string?> DisplayName { get; set; } = new();

    /// <summary>
    /// 描述（多语言，可选）- PostgreSQL jsonb 类型
    /// 示例：{"ja": "注文明細情報", "zh": "订单明细信息", "en": "Order line items"}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, string?>? Description { get; set; }

    /// <summary>
    /// 排序顺序（控制子实体在UI中的显示顺序）
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// 默认排序字段（子实体记录的默认排序字段名）
    /// </summary>
    [MaxLength(100)]
    public string? DefaultSortField { get; set; }

    /// <summary>
    /// 是否降序排序
    /// </summary>
    public bool IsDescending { get; set; } = false;

    /// <summary>
    /// 外键字段名（在子实体中指向主实体的外键字段，如 "OrderId"）
    /// 如果为空，则自动生成为 "{主实体名}Id"
    /// </summary>
    [MaxLength(100)]
    public string? ForeignKeyField { get; set; }

    /// <summary>
    /// 在主实体中的集合属性名（如 "Lines", "Comments"）
    /// 用于生成 AggVO 的子实体列表属性名
    /// 如果为空，则使用 Code
    /// </summary>
    [MaxLength(100)]
    public string? CollectionPropertyName { get; set; }

    /// <summary>
    /// 级联删除行为：NoAction、Cascade、SetNull、Restrict
    /// </summary>
    [MaxLength(20)]
    public string CascadeDeleteBehavior { get; set; } = "Cascade";

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    // ============ 导航属性 ============

    /// <summary>
    /// 字段集合（子实体的字段定义）
    /// </summary>
    public List<FieldMetadata> Fields { get; set; } = new();

    /// <summary>
    /// 导航属性：所属主实体
    /// </summary>
    public EntityDefinition? EntityDefinition { get; set; }

    // ============ 计算属性 ============

    /// <summary>
    /// 获取实际的集合属性名（优先使用 CollectionPropertyName，否则使用 Code）
    /// </summary>
    [NotMapped]
    public string ActualCollectionPropertyName => CollectionPropertyName ?? Code;

    /// <summary>
    /// 获取实际的外键字段名（优先使用 ForeignKeyField，否则自动生成）
    /// </summary>
    [NotMapped]
    public string ActualForeignKeyField => ForeignKeyField ?? $"{EntityDefinition?.EntityName}Id";
}
