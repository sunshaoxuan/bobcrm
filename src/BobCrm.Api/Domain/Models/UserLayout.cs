namespace BobCrm.Api.Domain;

public class UserLayout
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string? LayoutJson { get; set; }
}

