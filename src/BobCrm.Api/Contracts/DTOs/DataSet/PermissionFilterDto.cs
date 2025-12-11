namespace BobCrm.Api.Contracts.DTOs.DataSet;

/// <summary>
/// 权限过滤器 DTO
/// </summary>
public sealed record PermissionFilterDto
{
    public int Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public Dictionary<string, string?>? DisplayName { get; init; }
    public Dictionary<string, string?>? Description { get; init; }
    public string? RequiredFunctionCode { get; init; }
    public string? DataScopeTag { get; init; }
    public string? EntityType { get; init; }
    public string? FilterRulesJson { get; init; }
    public bool EnableFieldLevelPermissions { get; init; }
    public bool IsSystem { get; init; }
    public bool IsEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
