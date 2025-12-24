namespace BobCrm.App.Models;

public class MenuNodeOrderRequest
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public int SortOrder { get; set; }
}
