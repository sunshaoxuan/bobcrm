using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Domain;

/// <summary>
/// 本地化数据的基类，提供通用的本地化属性访问
/// </summary>
/// <typeparam name="TEntity">被本地化的实体类型</typeparam>
public abstract class LocalizationBase<TEntity> : ILocalizationData
    where TEntity : class
{
    [Key]
    public int EntityId { get; set; }
    
    [Required, MaxLength(8)]
    public string Language { get; set; } = string.Empty;
    
    public abstract string? GetLocalizedValue(string propertyName);
    
    /// <summary>
    /// 获取所有本地化的属性字典
    /// </summary>
    public abstract Dictionary<string, string?> GetAllLocalizedProperties();
}

