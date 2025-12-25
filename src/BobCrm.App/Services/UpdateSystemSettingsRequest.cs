using System.Text.Json.Serialization;

namespace BobCrm.App.Services;

public class UpdateSystemSettingsRequest
{
    [JsonPropertyName("companyName")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("defaultTheme")]
    public string? DefaultTheme { get; set; }

    [JsonPropertyName("defaultPrimaryColor")]
    public string? DefaultPrimaryColor { get; set; }

    [JsonPropertyName("defaultLanguage")]
    public string? DefaultLanguage { get; set; }

    [JsonPropertyName("defaultHomeRoute")]
    public string? DefaultHomeRoute { get; set; }

    [JsonPropertyName("defaultNavDisplayMode")]
    public string? DefaultNavDisplayMode { get; set; }

    [JsonPropertyName("timeZoneId")]
    public string? TimeZoneId { get; set; }

    [JsonPropertyName("allowSelfRegistration")]
    public bool? AllowSelfRegistration { get; set; }

    [JsonPropertyName("smtpHost")]
    public string? SmtpHost { get; set; }

    [JsonPropertyName("smtpPort")]
    public int? SmtpPort { get; set; }

    [JsonPropertyName("smtpUsername")]
    public string? SmtpUsername { get; set; }

    [JsonPropertyName("smtpPassword")]
    public string? SmtpPassword { get; set; }

    [JsonPropertyName("smtpEnableSsl")]
    public bool? SmtpEnableSsl { get; set; }

    [JsonPropertyName("smtpFromAddress")]
    public string? SmtpFromAddress { get; set; }

    [JsonPropertyName("smtpDisplayName")]
    public string? SmtpDisplayName { get; set; }
}
