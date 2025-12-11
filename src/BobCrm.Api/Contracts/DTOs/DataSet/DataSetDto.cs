namespace BobCrm.Api.Contracts.DTOs.DataSet;

/// <summary>
/// 数据集查询响应 DTO
/// </summary>
public sealed record DataSetDto
{
    public int Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public Dictionary<string, string?>? DisplayName { get; init; }
    public Dictionary<string, string?>? Description { get; init; }
    public required string DataSourceTypeCode { get; init; }
    public string? ConfigJson { get; init; }
    public string? FieldsJson { get; init; }
    public bool SupportsPaging { get; init; }
    public bool SupportsSorting { get; init; }
    public string? DefaultSortField { get; init; }
    public string DefaultSortDirection { get; init; } = "asc";
    public int DefaultPageSize { get; init; }
    public int? QueryDefinitionId { get; init; }
    public string? QueryDefinitionCode { get; init; }
    public int? PermissionFilterId { get; init; }
    public string? PermissionFilterCode { get; init; }
    public bool IsSystem { get; init; }
    public bool IsEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}
