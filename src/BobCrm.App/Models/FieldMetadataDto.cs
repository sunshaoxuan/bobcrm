namespace BobCrm.App.Models;

/// <summary>
/// 字段元数据DTO
/// </summary>
public class FieldMetadataDto
{
    public Guid Id { get; set; }
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// 显示名（多语言）- 从 API 加载的 jsonb 数据
    /// 前端使用 MultilingualTextDto，API 返回 Dictionary
    /// </summary>
    public MultilingualTextDto? DisplayName { get; set; }

    public string DataType { get; set; } = "String";
    public int? Length { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool IsRequired { get; set; }
    public bool IsEntityRef { get; set; }
    public Guid? ReferencedEntityId { get; set; }
    public string? TableName { get; set; }
    public int SortOrder { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }
    public Guid? EnumDefinitionId { get; set; }
    public bool IsMultiSelect { get; set; }
    public string? Source { get; set; }
}
