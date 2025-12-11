namespace BobCrm.Api.Contracts.Requests.DataSet;

/// <summary>
/// 更新权限过滤器请求
/// </summary>
public sealed record UpdatePermissionFilterRequest
{
    public string? Name { get; init; }
    public Dictionary<string, string?>? DisplayName { get; init; }
    public Dictionary<string, string?>? Description { get; init; }
    public string? RequiredFunctionCode { get; init; }
    public string? DataScopeTag { get; init; }
    public string? EntityType { get; init; }
    public string? FilterRulesJson { get; init; }
    public bool? EnableFieldLevelPermissions { get; init; }
    public bool? IsEnabled { get; init; }
}
