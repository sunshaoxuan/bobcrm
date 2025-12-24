namespace BobCrm.App.Models;

public class CreateOrganizationRequest
{
    public Guid? ParentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
