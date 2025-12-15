using System.Collections.Generic;

namespace BobCrm.Api.Services;

/// <summary>
/// 实体类型信息
/// </summary>
public class EntityTypeInfo
{
    public string FullName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public bool IsLoaded { get; set; }
    public List<PropertyTypeInfo> Properties { get; set; } = new();
    public List<string> Interfaces { get; set; } = new();
}
