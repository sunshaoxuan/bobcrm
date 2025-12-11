namespace BobCrm.Api.Contracts.Requests.DataSet;

/// <summary>
/// 创建权限过滤器请求
/// </summary>
public sealed record CreatePermissionFilterRequest
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public Dictionary<string, string?>? DisplayName { get; init; }
    public Dictionary<string, string?>? Description { get; init; }
    public string? RequiredFunctionCode { get; init; }
    public string? DataScopeTag { get; init; }
    public string? EntityType { get; init; }
    public string? FilterRulesJson { get; init; }
    public bool EnableFieldLevelPermissions { get; init; } = false;
}
