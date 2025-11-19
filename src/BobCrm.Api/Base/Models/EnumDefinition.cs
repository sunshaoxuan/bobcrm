using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Base.Models;

/// <summary>
/// 枚举定义 - 动态枚举系统的核心
/// 允许用户通过UI定义和管理枚举类型
/// </summary>
public class EnumDefinition
{
    /// <summary>枚举ID（主键）</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// 枚举代码（唯一标识，用于引用）
    /// 例如: "customer_type", "order_status"
    /// </summary>
    [Required, MaxLength(128)]
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// 枚举显示名（多语言JSON）
    /// 格式: {"zh": "客户类型", "en": "Customer Type", "ja": "顧客タイプ"}
    /// </summary>
    public Dictionary<string, string?> DisplayName { get; set; } = new();
    
    /// <summary>
    /// 枚举描述（多语言JSON）
    /// </summary>
    public Dictionary<string, string?> Description { get; set; } = new();
    
    /// <summary>
    /// 是否系统内置枚举
    /// 系统枚举不可删除，可用于内置业务逻辑
    /// </summary>
    public bool IsSystem { get; set; } = false;
    
    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>最后更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>创建人ID</summary>
    public string? CreatedBy { get; set; }
    
    /// <summary>更新人ID</summary>
    public string? UpdatedBy { get; set; }
    
    /// <summary>枚举选项集合</summary>
    public List<EnumOption> Options { get; set; } = new();
}
