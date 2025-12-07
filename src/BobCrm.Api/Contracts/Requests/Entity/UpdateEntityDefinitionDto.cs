using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.Api.Contracts.Requests.Entity;

public record UpdateEntityDefinitionDto
{
    public string? Namespace { get; init; }
    public string? EntityName { get; init; }

    /// <summary>
    /// 显示名（多语言）
    /// </summary>
    public MultilingualText? DisplayName { get; init; }

    /// <summary>
    /// 描述（多语言）
    /// </summary>
    public MultilingualText? Description { get; init; }

    public string? StructureType { get; init; }
    public List<UpdateFieldMetadataDto>? Fields { get; init; }
    public List<string>? Interfaces { get; init; }
}

public record UpdateFieldMetadataDto
{
    public Guid? Id { get; init; }
    public string? PropertyName { get; init; }

    /// <summary>
    /// 显示名（多语言）
    /// </summary>
    public MultilingualText? DisplayName { get; init; }

    public string? DataType { get; init; }
    public int? Length { get; init; }
    public int? Precision { get; init; }
    public int? Scale { get; init; }
    public bool? IsRequired { get; init; }
    public bool? IsEntityRef { get; init; }
    public Guid? ReferencedEntityId { get; init; }
    public int? SortOrder { get; init; }
    public string? DefaultValue { get; init; }
    public string? ValidationRules { get; init; }
}
