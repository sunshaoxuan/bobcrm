namespace BobCrm.Api.Contracts.Responses.Entity;

/// <summary>
/// 属性类型信息 DTO。
/// </summary>
public class PropertyTypeInfoDto
{
    /// <summary>
    /// 属性名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 属性类型名称。
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// 是否可空（Nullable）。
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// 是否可读。
    /// </summary>
    public bool CanRead { get; set; }

    /// <summary>
    /// 是否可写。
    /// </summary>
    public bool CanWrite { get; set; }
}

