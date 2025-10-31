namespace BobCrm.Api.Domain;

public class UserPreferences
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? Theme { get; set; } = "light"; // "light" or "dark"
    public string? PrimaryColor { get; set; } = "#3f7cff";
    public string? Language { get; set; } = "ja";
    public DateTime? UpdatedAt { get; set; }
}
