namespace BobCrm.App.Models;

public class EntityDomainDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public MultilingualTextDto? Name { get; set; } = new();
    public int SortOrder { get; set; }
    public bool IsSystem { get; set; }
}

