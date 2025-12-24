namespace BobCrm.App.Models;

public class CreateMenuNodeRequest
{
    public Guid? ParentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public MultilingualTextDto? DisplayName { get; set; }
    public string? Route { get; set; }
    public string? Icon { get; set; }
    public bool IsMenu { get; set; } = true;
    public int SortOrder { get; set; } = 100;
    public int? TemplateId { get; set; }
}
