namespace BobCrm.Api.Services;

/// <summary>
/// 属性类型信息
/// </summary>
public class PropertyTypeInfo
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
}
