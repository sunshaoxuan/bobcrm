namespace BobCrm.App.Models;

public class RoleDataScopeDto
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string ScopeType { get; set; } = string.Empty;
    public string? FilterExpression { get; set; }
}
