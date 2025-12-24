namespace BobCrm.App.Models;

public class CreateRoleRequestDto
{
    public Guid? OrganizationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<Guid> FunctionIds { get; set; } = new();
    public List<RoleDataScopeDto> DataScopes { get; set; } = new();
}
