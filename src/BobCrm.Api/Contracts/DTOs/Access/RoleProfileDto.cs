namespace BobCrm.Api.Contracts.DTOs.Access;

/// <summary>
/// 角色档案 DTO
/// </summary>
public record RoleProfileDto
{
    public Guid Id { get; init; }
    public Guid? OrganizationId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
    public bool IsEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<RoleFunctionDto> Functions { get; init; } = new();
    public List<RoleDataScopeDto> DataScopes { get; init; } = new();
}
