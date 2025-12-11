namespace BobCrm.Api.Contracts.Requests.DataSet;

/// <summary>
/// 更新查询定义请求
/// </summary>
public sealed record UpdateQueryDefinitionRequest
{
    public string? Name { get; init; }
    public Dictionary<string, string?>? DisplayName { get; init; }
    public Dictionary<string, string?>? Description { get; init; }
    public string? ConditionsJson { get; init; }
    public string? ParametersJson { get; init; }
    public string? AggregationsJson { get; init; }
    public string? GroupByFields { get; init; }
    public bool? IsEnabled { get; init; }
}
