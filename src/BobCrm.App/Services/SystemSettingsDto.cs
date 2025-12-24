using System.Text.Json.Serialization;

namespace BobCrm.App.Services;

public class SystemSettingsDto
{
    [JsonPropertyName("companyName")]
    public string CompanyName { get; set; } = "OneCRM";

    [JsonPropertyName("defaultTheme")]
    public string DefaultTheme { get; set; } = "calm-light";

    [JsonPropertyName("defaultPrimaryColor")]
    public string? DefaultPrimaryColor { get; set; } = "#739FD6";

    [JsonPropertyName("defaultLanguage")]
    public string DefaultLanguage { get; set; } = "ja";

    [JsonPropertyName("defaultHomeRoute")]
    public string DefaultHomeRoute { get; set; } = "/";

    [JsonPropertyName("defaultNavDisplayMode")]
    public string DefaultNavDisplayMode { get; set; } = "icon-text";

    [JsonPropertyName("timeZoneId")]
    public string TimeZoneId { get; set; } = "Asia/Tokyo";

    [JsonPropertyName("allowSelfRegistration")]
    public bool AllowSelfRegistration { get; set; }
}
