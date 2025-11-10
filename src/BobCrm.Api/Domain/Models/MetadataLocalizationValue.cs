using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BobCrm.Api.Domain.Models;

/// <summary>
/// 元数据多语言资源表（动态结构）
/// 用于存储实体定义和字段的多语言文本
/// </summary>
[Table("MetadataLocalizationValues")]
public class MetadataLocalizationValue
{
    /// <summary>
    /// 资源Key（如：ENTITY_PRODUCT、FIELD_PRODUCT_PRICE）
    /// </summary>
    [Key, Column(Order = 0), MaxLength(256)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 语言代码（如：ja、zh、en）
    /// 从 LocalizationLanguages 表动态获取
    /// </summary>
    [Key, Column(Order = 1), MaxLength(8)]
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// 本地化的文本值
    /// </summary>
    [Required]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
