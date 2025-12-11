namespace BobCrm.Api.Contracts.Requests.DataSet;

/// <summary>
/// 更新数据集请求
/// </summary>
public sealed record UpdateDataSetRequest
{
    public required string Name { get; init; }
    public Dictionary<string, string?>? DisplayName { get; init; }
    public Dictionary<string, string?>? Description { get; init; }
    public string? ConfigJson { get; init; }
    public string? FieldsJson { get; init; }
    public bool SupportsPaging { get; init; } = true;
    public bool SupportsSorting { get; init; } = true;
    public string? DefaultSortField { get; init; }
    public string DefaultSortDirection { get; init; } = "asc";
    public int DefaultPageSize { get; init; } = 20;
    public int? QueryDefinitionId { get; init; }
    public int? PermissionFilterId { get; init; }
    public bool IsEnabled { get; init; } = true;
    public string? UpdatedBy { get; init; }
}
