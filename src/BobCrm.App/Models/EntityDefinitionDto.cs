namespace BobCrm.App.Models;

/// <summary>
/// 实体定义DTO
/// </summary>
public class EntityDefinitionDto
{
    public Guid Id { get; set; }
    public string Namespace { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string FullTypeName { get; set; } = string.Empty;
    public string EntityRoute { get; set; } = string.Empty;

    /// <summary>
    /// 显示名（多语言）- 从 API 加载的 jsonb 数据
    /// </summary>
    public Dictionary<string, string?>? DisplayName { get; set; }

    /// <summary>
    /// 描述（多语言）- 从 API 加载的 jsonb 数据
    /// </summary>
    public Dictionary<string, string?>? Description { get; set; }

    public string ApiEndpoint { get; set; } = string.Empty;
    public string StructureType { get; set; } = "Single";
    public string Status { get; set; } = "Draft";
    public string Source { get; set; } = "Custom";
    public bool IsLocked { get; set; }
    public bool IsRootEntity { get; set; }
    public bool IsEnabled { get; set; } = true;
    public int Order { get; set; }
    public string? Icon { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public int FieldCount { get; set; }
    public List<FieldMetadataDto> Fields { get; set; } = new();
    public List<EntityInterfaceDto> Interfaces { get; set; } = new();
}
