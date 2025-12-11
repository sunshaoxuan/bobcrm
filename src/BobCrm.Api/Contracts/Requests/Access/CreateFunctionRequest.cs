using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.Api.Contracts.Requests.Access;

/// <summary>
/// 创建功能请求
/// </summary>
public record CreateFunctionRequest
{
    public Guid? ParentId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public MultilingualText? DisplayName { get; init; }
    public string? Route { get; init; }
    public string? Icon { get; init; }
    public bool IsMenu { get; init; } = true;
    public int SortOrder { get; init; } = 100;
    public int? TemplateId { get; init; }
}
