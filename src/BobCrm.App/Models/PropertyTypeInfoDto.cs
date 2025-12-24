namespace BobCrm.App.Models;

/// <summary>
/// 属性类型信息DTO
/// </summary>
public class PropertyTypeInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
}
