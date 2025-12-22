using System.Text.Json.Serialization;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Contracts.Responses.Entity;

/// <summary>
/// 字段元数据 DTO
/// </summary>
public class FieldMetadataDto
{
    public Guid Id { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    /// <summary>
    /// 显示名资源Key（接口字段），用于调试和回溯，始终返回
    /// </summary>
    public string? DisplayNameKey { get; set; }
    /// <summary>
    /// 单语显示名（单语模式）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }
    /// <summary>
    /// 多语显示名（向后兼容）
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MultilingualText? DisplayNameTranslations { get; set; }
    public string DataType { get; set; } = string.Empty;
    public int? Length { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool IsRequired { get; set; }
    public bool IsEntityRef { get; set; }
    public Guid? ReferencedEntityId { get; set; }
    public string? LookupEntityName { get; set; }
    public string? LookupDisplayField { get; set; }
    public ForeignKeyAction ForeignKeyAction { get; set; }
    public string? TableName { get; set; }
    public int SortOrder { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }
    public string Source { get; set; } = string.Empty;
    public Guid? EnumDefinitionId { get; set; }
    public bool IsMultiSelect { get; set; }
}
