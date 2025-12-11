namespace BobCrm.Api.Contracts.Requests.Access;

/// <summary>
/// 创建角色请求
/// </summary>
public record CreateRoleRequest
{
    public Guid? OrganizationId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsEnabled { get; init; } = true;
    public List<Guid> FunctionIds { get; init; } = new();
    public List<BobCrm.Api.Contracts.DTOs.Access.RoleDataScopeDto> DataScopes { get; init; } = new();
}
