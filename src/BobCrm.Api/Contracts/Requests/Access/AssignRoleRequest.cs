namespace BobCrm.Api.Contracts.Requests.Access;

/// <summary>
/// 分配角色请求
/// </summary>
public record AssignRoleRequest
{
    public string UserId { get; init; } = string.Empty;
    public Guid RoleId { get; init; }
    public Guid? OrganizationId { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
}
