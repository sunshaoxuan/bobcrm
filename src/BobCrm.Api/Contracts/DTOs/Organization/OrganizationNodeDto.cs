namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 组织节点 DTO
/// </summary>
public record OrganizationNodeDto
{
    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string PathCode { get; init; } = string.Empty;
    public int Level { get; init; }
    public int SortOrder { get; init; }
    public List<OrganizationNodeDto> Children { get; init; } = new();
}
