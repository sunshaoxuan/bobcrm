namespace BobCrm.Api.Contracts.DTOs;

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

public record CreateOrganizationRequest
{
    public Guid? ParentId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public record UpdateOrganizationRequest
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
