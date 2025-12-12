using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.Api.Contracts.Requests.Entity;

/// <summary>
/// 创建实体定义请求
/// </summary>
public record CreateEntityDefinitionDto
{
    public string Namespace { get; init; } = "BobCrm.Base.Custom";
    public string EntityName { get; init; } = string.Empty;

    /// <summary>
    /// 显示名（多语言）
    /// </summary>
    public MultilingualText? DisplayName { get; init; }

    /// <summary>
    /// 描述（多语言）
    /// </summary>
    public MultilingualText? Description { get; init; }

    public string? StructureType { get; init; }
    public List<CreateFieldMetadataDto>? Fields { get; init; }
    public List<string>? Interfaces { get; init; }
}
