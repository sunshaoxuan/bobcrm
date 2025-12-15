namespace BobCrm.Api.Endpoints;

/// <summary>
/// 字段元数据DTO（用于API传输）
/// </summary>
public class FieldMetadataDto
{
    public Guid Id { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public Dictionary<string, string?> DisplayName { get; set; } = new();
    public string DataType { get; set; } = string.Empty;
    public int? Length { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }
    public int SortOrder { get; set; }
}

