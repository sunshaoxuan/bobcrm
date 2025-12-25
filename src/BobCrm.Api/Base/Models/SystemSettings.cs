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

    // SMTP / Email
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 25;
    public string? SmtpUsername { get; set; }
    public string? SmtpPasswordEncrypted { get; set; }
    public bool SmtpEnableSsl { get; set; } = false;
    public string? SmtpFromAddress { get; set; }
    public string? SmtpDisplayName { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
