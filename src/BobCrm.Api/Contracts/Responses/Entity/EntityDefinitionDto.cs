using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.Api.Contracts.Responses.Entity;

/// <summary>
/// 实体定义完整响应 DTO
/// </summary>
public class EntityDefinitionDto
{
    public Guid Id { get; set; }
    public string Namespace { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string FullTypeName { get; set; } = string.Empty;
    public string EntityRoute { get; set; } = string.Empty;
    public MultilingualText? DisplayName { get; set; }
    public MultilingualText? Description { get; set; }
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
    public string? UpdatedBy { get; set; }
    public List<FieldMetadataDto> Fields { get; set; } = new();
    public List<EntityInterfaceDto> Interfaces { get; set; } = new();
}
