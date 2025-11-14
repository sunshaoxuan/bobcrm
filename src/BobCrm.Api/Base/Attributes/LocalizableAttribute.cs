namespace BobCrm.Api.Domain.Attributes;

/// <summary>
/// 标记属性可以被本地化
/// 在实体类中标记此特性的属性将被自动纳入本地化系统
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class LocalizableAttribute : Attribute
{
    /// <summary>
    /// 是否必需（默认为true，如果为false则在找不到本地化值时使用默认值）
    /// </summary>
    public bool Required { get; set; } = true;
    
    /// <summary>
    /// 最大长度限制
    /// </summary>
    public int MaxLength { get; set; } = -1;
    
    /// <summary>
    /// 本地化时的提示信息
    /// </summary>
    public string? Hint { get; set; }
}

