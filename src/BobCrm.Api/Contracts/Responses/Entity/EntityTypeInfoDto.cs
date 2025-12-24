namespace BobCrm.Api.Contracts.Responses.Entity;

/// <summary>
/// 实体类型信息 DTO。
/// 用于描述已加载动态实体的类型元数据（属性、接口等）。
/// </summary>
public class EntityTypeInfoDto
{
    /// <summary>
    /// 完整类型名称（包含命名空间）。
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// 类型名称（不包含命名空间）。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 命名空间。
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// 是否已加载到运行时。
    /// </summary>
    public bool IsLoaded { get; set; }

    /// <summary>
    /// 公共属性列表。
    /// </summary>
    public List<PropertyTypeInfoDto> Properties { get; set; } = new();

    /// <summary>
    /// 实现的接口名称列表。
    /// </summary>
    public List<string> Interfaces { get; set; } = new();
}

