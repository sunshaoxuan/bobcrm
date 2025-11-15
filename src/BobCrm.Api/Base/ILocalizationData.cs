namespace BobCrm.Api.Base;

/// <summary>
/// 本地化数据实体必须实现的接口
/// </summary>
public interface ILocalizationData
{
    /// <summary>
    /// 所属实体的ID
    /// </summary>
    int EntityId { get; set; }
    
    /// <summary>
    /// 语言代码
    /// </summary>
    string Language { get; set; }
    
    /// <summary>
    /// 获取本地化的属性值（如Name、Description等）
    /// </summary>
    /// <param name="propertyName">属性名</param>
    /// <returns>本地化的值</returns>
    string? GetLocalizedValue(string propertyName);
}

