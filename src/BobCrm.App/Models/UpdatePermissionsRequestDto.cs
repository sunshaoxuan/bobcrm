namespace BobCrm.App.Models;

public class UpdatePermissionsRequestDto
{
    public List<Guid> FunctionIds { get; set; } = new();
    public List<RoleDataScopeDto> DataScopes { get; set; } = new();
    public List<FunctionPermissionSelectionDto> FunctionPermissions { get; set; } = new();
}
