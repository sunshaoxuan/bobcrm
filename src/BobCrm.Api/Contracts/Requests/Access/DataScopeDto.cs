namespace BobCrm.Api.Contracts.Requests.Access;

/// <summary>
/// 数据范围请求 DTO
/// </summary>
public record DataScopeDto(string EntityName, string ScopeType, string? FilterExpression);
