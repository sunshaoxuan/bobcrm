namespace BobCrm.App.Models;

/// <summary>
/// 实体类型信息响应
/// </summary>
public class EntityTypeInfoResponse
{
    public string FullName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public bool IsLoaded { get; set; }
    public List<PropertyTypeInfoDto> Properties { get; set; } = new();
    public List<string> Interfaces { get; set; } = new();
}
