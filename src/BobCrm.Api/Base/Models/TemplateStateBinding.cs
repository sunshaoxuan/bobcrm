using System;
using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Base.Models;

/// <summary>
/// 模板与状态的绑定关系
/// 实现模板和视图状态的 N:M 关系
/// 一个模板可以被多个状态共享，一个状态也可以有多个模板（但只能有一个默认模板）
/// </summary>
public class TemplateStateBinding
{
    /// <summary>绑定记录 ID（主键）</summary>
    public int Id { get; set; }
    
    /// <summary>
    /// 实体类型（如 "customer", "order"）
    /// </summary>
    public string EntityType { get; set; } = string.Empty;
    
    /// <summary>
    /// 视图状态值（引用 EnumOption.Value）
    /// 引用 "view_state" 枚举的选项值，如 "List", "DetailView", "DetailEdit", "Create"
    /// </summary>
    [Required, MaxLength(64)]
    public string ViewState { get; set; } = string.Empty;
    
    /// <summary>
    /// 关联的模板 ID
    /// </summary>
    public int TemplateId { get; set; }

    /// <summary>
    /// 条件匹配字段名（如 "Status"）
    /// 为空时视为该 ViewState 的通用/默认绑定（向后兼容）
    /// </summary>
    [MaxLength(128)]
    public string? MatchFieldName { get; set; }

    /// <summary>
    /// 条件匹配字段值（如 "Draft"）
    /// </summary>
    [MaxLength(256)]
    public string? MatchFieldValue { get; set; }

    /// <summary>
    /// 匹配优先级（值越大越优先）
    /// </summary>
    public int Priority { get; set; } = 0;
    
    /// <summary>
    /// 是否为该实体+状态的默认模板
    /// 每个实体的每个状态只能有一个默认模板
    /// 数据库通过唯一索引保证: UNIQUE(EntityType, ViewState, IsDefault) WHERE IsDefault = 1
    /// </summary>
    public bool IsDefault { get; set; }
    
    /// <summary>
    /// 访问所需权限（可选）
    /// 例如："customer.edit", "order.approve"
    /// </summary>
    public string? RequiredPermission { get; set; }
    
    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // ========== 导航属性 ==========
    
    /// <summary>
    /// 关联的模板
    /// </summary>
    public FormTemplate Template { get; set; } = null!;
}
