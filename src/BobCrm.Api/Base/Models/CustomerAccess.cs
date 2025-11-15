namespace BobCrm.Api.Base;

public class CustomerAccess
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool CanEdit { get; set; }
}

