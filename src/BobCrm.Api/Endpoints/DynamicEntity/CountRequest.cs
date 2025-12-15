using BobCrm.Api.Services;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// 统计请求DTO
/// </summary>
public record CountRequest
{
    public List<FilterCondition>? Filters { get; init; }
}

