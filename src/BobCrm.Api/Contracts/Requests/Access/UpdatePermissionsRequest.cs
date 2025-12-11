namespace BobCrm.Api.Contracts.Requests.Access;

/// <summary>
/// 更新角色权限请求
/// </summary>
public record UpdatePermissionsRequest
{
    public List<Guid>? FunctionIds { get; init; }
    public List<DataScopeDto>? DataScopes { get; init; }
    public List<FunctionPermissionSelectionDto>? FunctionPermissions { get; init; }
}
