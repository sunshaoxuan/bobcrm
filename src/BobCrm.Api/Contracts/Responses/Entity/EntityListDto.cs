using System.Text.Json.Serialization;
using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.Api.Contracts.Responses.Entity;

/// <summary>
/// 实体列表项 DTO - 用于管理界面
/// </summary>
public class EntityListDto
{
    public Guid Id { get; set; }
    public string Namespace { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string FullTypeName { get; set; } = string.Empty;
    public string EntityRoute { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MultilingualText? DisplayNameTranslations { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MultilingualText? DescriptionTranslations { get; set; }

    public string? ApiEndpoint { get; set; }
    public string StructureType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public bool IsRootEntity { get; set; }
    public bool IsEnabled { get; set; }
    public int Order { get; set; }
    public string? Icon { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public int FieldCount { get; set; }
    public List<EntityInterfaceDto> Interfaces { get; set; } = new();
}
