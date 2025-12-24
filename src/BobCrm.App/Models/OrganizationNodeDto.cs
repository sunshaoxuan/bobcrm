namespace BobCrm.App.Models;

public class OrganizationNodeDto
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PathCode { get; set; } = string.Empty;
    public int Level { get; set; }
    public int SortOrder { get; set; }
    public List<OrganizationNodeDto> Children { get; set; } = new();
}
