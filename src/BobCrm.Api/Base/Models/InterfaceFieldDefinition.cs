namespace BobCrm.Api.Base.Models;

/// <summary>
/// 接口字段定义
/// </summary>
public class InterfaceFieldDefinition
{
    public string PropertyName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int? Length { get; set; }
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsEntityRef { get; set; }
    public string? ReferenceTable { get; set; }
}
