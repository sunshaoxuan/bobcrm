namespace BobCrm.App.Models;

public class UpdateMenuNodeRequest
{
    public Guid? ParentId { get; set; }
    public bool ClearParent { get; set; }
    public string? Name { get; set; }
    public MultilingualTextDto? DisplayName { get; set; }
    public string? Route { get; set; }
    public bool ClearRoute { get; set; }
    public string? Icon { get; set; }
    public bool? IsMenu { get; set; }
    public int? SortOrder { get; set; }
    public int? TemplateId { get; set; }
    public bool ClearTemplate { get; set; }
}
