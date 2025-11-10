using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BobCrm.Api.Domain.Models;

/// <summary>
/// 统一的实体定义 - 既是元数据又是注册表
/// 所有业务实体（系统内置+用户自定义）都通过此表管理
/// </summary>
public class EntityDefinition
{
    // ============ 主键和标识 ============

    /// <summary>
    /// 实体定义ID（主键）
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 实体全名（唯一标识，如 BobCrm.Domain.Customer）
    /// 用于反射查找和类型匹配
    /// </summary>
    [Required, MaxLength(500)]
    public string FullTypeName { get; set; } = string.Empty;

    /// <summary>
    /// 命名空间（如 BobCrm.Domain）
    /// </summary>
    [Required, MaxLength(500)]
    public string Namespace { get; set; } = "BobCrm.Domain.Custom";

    /// <summary>
    /// 实体名（如 Customer）
    /// </summary>
    [Required, MaxLength(100)]
    public string EntityName { get; set; } = string.Empty;

    // ============ 显示和路由 ============

    /// <summary>
    /// URL路由名（如 customer，小写）
    /// 用于动态路由 /{entityRoute}/{id}
    /// </summary>
    [Required, MaxLength(100)]
    public string EntityRoute { get; set; } = string.Empty;

    /// <summary>
    /// 显示名（多语言）- PostgreSQL jsonb 类型
    /// 示例：{"ja": "商品", "zh": "产品", "en": "Product"}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, string>? DisplayName { get; set; }

    /// <summary>
    /// 描述（多语言）- PostgreSQL jsonb 类型
    /// 示例：{"ja": "商品情報", "zh": "产品信息", "en": "Product info"}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, string>? Description { get; set; }

    /// <summary>
    /// API端点基础路径（如 /api/customers）
    /// </summary>
    [Required, MaxLength(200)]
    public string ApiEndpoint { get; set; } = string.Empty;

    // ============ 结构和状态 ============

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

    // ============ 主子表结构配置 ============

    /// <summary>
    /// 父实体ID（仅用于 MasterDetail 和 MasterDetailGrandchild 的子实体）
    /// Detail 实体指向 Master，Grandchild 实体指向 Detail
    /// </summary>
    public Guid? ParentEntityId { get; set; }

    /// <summary>
    /// 父实体名称（冗余字段，便于查询和显示）
    /// </summary>
    [MaxLength(100)]
    public string? ParentEntityName { get; set; }

    /// <summary>
    /// 父实体外键字段名（在子实体中，如 "OrderId"、"OrderLineId"）
    /// </summary>
    [MaxLength(100)]
    public string? ParentForeignKeyField { get; set; }

    /// <summary>
    /// 在父实体中的集合属性名（如 "OrderLines"、"Comments"）
    /// 用于生成 AggVO 的子实体列表属性名
    /// </summary>
    [MaxLength(100)]
    public string? ParentCollectionProperty { get; set; }

    /// <summary>
    /// 级联删除行为：NoAction、Cascade、SetNull、Restrict
    /// </summary>
    [MaxLength(20)]
    public string CascadeDeleteBehavior { get; set; } = "NoAction";

    /// <summary>
    /// 是否自动级联保存（保存主表时自动保存子表）
    /// </summary>
    public bool AutoCascadeSave { get; set; } = true;

    // ============ 注册表属性（原EntityMetadata字段）============

    /// <summary>
    /// 是否为根实体（只有根实体可创建模板）
    /// </summary>
    public bool IsRootEntity { get; set; } = true;

    /// <summary>
    /// 是否启用（可用于临时禁用某些实体）
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 排序顺序（用于UI显示排序）
    /// </summary>
    public int Order { get; set; } = 0;

    /// <summary>
    /// 图标（可选，用于UI显示，如 user、database、shopping-cart）
    /// </summary>
    [MaxLength(50)]
    public string? Icon { get; set; }

    /// <summary>
    /// 实体分类（如：core、sales、service、custom）
    /// </summary>
    [MaxLength(50)]
    public string? Category { get; set; }

    // ============ 来源标识 ============

    /// <summary>
    /// 实体来源：System（系统初始化）或 Custom（用户创建）
    /// System实体在启动时从代码同步，Custom实体完全由用户创建
    /// </summary>
    [Required, MaxLength(50)]
    public string Source { get; set; } = "Custom";

    // ============ 审计字段 ============

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

    // ============ 导航属性 ============

    /// <summary>
    /// 字段元数据集合
    /// </summary>
    public List<FieldMetadata> Fields { get; set; } = new();

    /// <summary>
    /// 实体接口集合
    /// </summary>
    public List<EntityInterface> Interfaces { get; set; } = new();

    /// <summary>
    /// DDL脚本集合
    /// </summary>
    public List<DDLScript> DDLScripts { get; set; } = new();

    // ============ 计算属性 ============

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

/// <summary>
/// 实体来源枚举
/// </summary>
public static class EntitySource
{
    /// <summary>系统初始化实体（从代码同步）</summary>
    public const string System = "System";

    /// <summary>用户自定义实体（通过UI创建）</summary>
    public const string Custom = "Custom";
}

/// <summary>
/// 级联删除行为枚举
/// </summary>
public static class CascadeDeleteBehavior
{
    /// <summary>不执行任何操作</summary>
    public const string NoAction = "NoAction";

    /// <summary>级联删除相关记录</summary>
    public const string Cascade = "Cascade";

    /// <summary>将外键设置为NULL</summary>
    public const string SetNull = "SetNull";

    /// <summary>阻止删除（抛出错误）</summary>
    public const string Restrict = "Restrict";
}
