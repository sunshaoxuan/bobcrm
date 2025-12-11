namespace BobCrm.Api.Contracts.DTOs.DataSet;

/// <summary>
/// 查询定义 DTO
/// </summary>
public sealed record QueryDefinitionDto
{
    public int Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public Dictionary<string, string?>? DisplayName { get; init; }
    public Dictionary<string, string?>? Description { get; init; }
    public string? ConditionsJson { get; init; }
    public string? ParametersJson { get; init; }
    public string? AggregationsJson { get; init; }
    public string? GroupByFields { get; init; }
    public bool IsEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
