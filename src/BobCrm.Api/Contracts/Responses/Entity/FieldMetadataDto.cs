using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.Api.Contracts.Responses.Entity;

/// <summary>
/// 字段元数据 DTO
/// </summary>
public class FieldMetadataDto
{
    public Guid Id { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public MultilingualText? DisplayName { get; set; }
    public string DataType { get; set; } = string.Empty;
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
    public string Source { get; set; } = string.Empty;
    public Guid? EnumDefinitionId { get; set; }
    public bool IsMultiSelect { get; set; }
}
