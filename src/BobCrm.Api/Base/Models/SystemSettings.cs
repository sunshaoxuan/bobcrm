namespace BobCrm.Api.Base.Models;

public class SystemSettings
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = "OneCRM";
    public string DefaultTheme { get; set; } = "calm-light";
    public string? DefaultPrimaryColor { get; set; } = "#739FD6";
    public string DefaultLanguage { get; set; } = "ja";
    public string DefaultHomeRoute { get; set; } = "/";
    public string DefaultNavMode { get; set; } = NavDisplayModes.IconText;
    public string TimeZoneId { get; set; } = "Asia/Tokyo";
    public bool AllowSelfRegistration { get; set; } = false;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
