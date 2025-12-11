using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Contracts.DTOs.Access;

/// <summary>
/// 角色数据范围 DTO
/// </summary>
public record RoleDataScopeDto
{
    public Guid Id { get; init; }
    public string EntityName { get; init; } = string.Empty;
    public string ScopeType { get; init; } = RoleDataScopeTypes.All;
    public string? FilterExpression { get; init; }
}
