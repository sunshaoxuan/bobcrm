namespace BobCrm.App.Models;

public class UpdateRoleRequestDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsEnabled { get; set; }
}
