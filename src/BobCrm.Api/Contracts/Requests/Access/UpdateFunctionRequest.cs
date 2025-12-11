using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.Api.Contracts.Requests.Access;

/// <summary>
/// 更新功能请求
/// </summary>
public record UpdateFunctionRequest
{
    public Guid? ParentId { get; init; }
    public bool ClearParent { get; init; }
    public string? Name { get; init; }
    public MultilingualText? DisplayName { get; init; }
    public string? Route { get; init; }
    public bool ClearRoute { get; init; }
    public string? Icon { get; init; }
    public bool? IsMenu { get; init; }
    public int? SortOrder { get; init; }
    public int? TemplateId { get; init; }
    public bool ClearTemplate { get; init; }
}
