using BobCrm.Api.Services;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// 查询请求DTO
/// </summary>
public record QueryRequest
{
    public List<FilterCondition>? Filters { get; init; }
    public string? OrderBy { get; init; }
    public bool OrderByDescending { get; init; }
    public int? Skip { get; init; }
    public int? Take { get; init; }
}

