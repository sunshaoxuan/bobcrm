using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Base.Models;

/// <summary>
/// 枚举选项 - 动态枚举的具体选项值
/// </summary>
public class EnumOption
{
    /// <summary>选项ID（主键）</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>所属枚举定义ID（外键）</summary>
    public Guid EnumDefinitionId { get; set; }
    
    /// <summary>
    /// 选项值（存储到数据库字段的实际值）
    /// 例如: "ENTERPRISE", "INDIVIDUAL", "HIGH", "LOW"
    /// 建议使用大写英文，一旦创建不应修改（影响历史数据）
    /// </summary>
    [Required, MaxLength(64)]
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// 选项显示名（多语言JSON）
    /// 格式: {"zh": "企业客户", "en": "Enterprise", "ja": "企業顧客"}
    /// </summary>
    public Dictionary<string, string?> DisplayName { get; set; } = new();
    
    /// <summary>
    /// 选项描述（多语言JSON，可选）
    /// </summary>
    public Dictionary<string, string?> Description { get; set; } = new();
    
    /// <summary>
    /// 显示顺序（用于UI排序）
    /// 数字越小越靠前
    /// </summary>
    public int SortOrder { get; set; }
    
    /// <summary>是否启用（禁用的选项不在UI中显示）</summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// 是否系统必需选项
    /// 系统选项不可通过UI删除或修改，启动时检查并自动重建
    /// </summary>
    public bool IsSystem { get; set; } = false;
    
    /// <summary>
    /// 颜色标记（可选，用于UI显示）
    /// 例如: "red", "green", "blue", "#FF5733"
    /// </summary>
    [MaxLength(16)]
    public string? ColorTag { get; set; }
    
    /// <summary>
    /// 图标名称（可选，用于UI显示）
    /// 例如: "check", "warning", "star"
    /// </summary>
    [MaxLength(64)]
    public string? Icon { get; set; }
    
    /// <summary>导航属性 - 所属枚举定义</summary>
    public EnumDefinition EnumDefinition { get; set; } = null!;
}
