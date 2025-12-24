namespace BobCrm.App.Models;

public class RoleProfileDto
{
    public Guid Id { get; set; }
    public Guid? OrganizationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<RoleFunctionDto> Functions { get; set; } = new();
    public List<RoleDataScopeDto> DataScopes { get; set; } = new();
}
