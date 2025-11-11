namespace BobCrm.App.Models;

/// <summary>
/// 实体定义聚合DTO
/// </summary>
public class EntityDefinitionAggregateDto
{
    /// <summary>
    /// 主实体
    /// </summary>
    public EntityDefinitionDto Master { get; set; } = null!;

    /// <summary>
    /// 子实体列表
    /// </summary>
    public List<SubEntityDto> SubEntities { get; set; } = new();
}

/// <summary>
/// 子实体DTO
/// </summary>
public class SubEntityDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Dictionary<string, string?> DisplayName { get; set; } = new();
    public Dictionary<string, string?>? Description { get; set; }
    public int SortOrder { get; set; }
    public string? DefaultSortField { get; set; }
    public bool IsDescending { get; set; }
    public string? ForeignKeyField { get; set; }
    public string? CollectionPropertyName { get; set; }
    public string CascadeDeleteBehavior { get; set; } = "Cascade";
    public List<FieldMetadataDto> Fields { get; set; } = new();
}

/// <summary>
/// 保存聚合请求
/// </summary>
public class SaveEntityDefinitionAggregateRequest
{
    public Guid Id { get; set; }
    public string Namespace { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public Dictionary<string, string?> DisplayName { get; set; } = new();
    public Dictionary<string, string?>? Description { get; set; }
    public List<SubEntityDto> SubEntities { get; set; } = new();
}
