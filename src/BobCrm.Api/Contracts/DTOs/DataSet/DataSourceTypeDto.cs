namespace BobCrm.Api.Contracts.DTOs.DataSet;

/// <summary>
/// 数据源类型 DTO
/// </summary>
public sealed record DataSourceTypeDto
{
    public Guid Id { get; init; }
    public required string Code { get; init; }
    public Dictionary<string, string?>? DisplayName { get; init; }
    public Dictionary<string, string?>? Description { get; init; }
    public required string HandlerType { get; init; }
    public string? ConfigSchema { get; init; }
    public string Category { get; init; } = "General";
    public string? Icon { get; init; }
    public bool IsSystem { get; init; }
    public bool IsEnabled { get; init; }
    public int SortOrder { get; init; }
}
