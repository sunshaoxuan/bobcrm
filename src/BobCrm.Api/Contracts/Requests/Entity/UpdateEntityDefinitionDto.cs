using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.Api.Contracts.Requests.Entity;

/// <summary>
/// 更新实体定义请求
/// </summary>
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
