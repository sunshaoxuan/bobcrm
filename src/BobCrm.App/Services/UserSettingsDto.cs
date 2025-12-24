using System.Text.Json.Serialization;

namespace BobCrm.App.Services;

public class UserSettingsDto
{
    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "calm-light";

    [JsonPropertyName("primaryColor")]
    public string? PrimaryColor { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; } = "ja";

    [JsonPropertyName("homeRoute")]
    public string HomeRoute { get; set; } = "/";

    [JsonPropertyName("navDisplayMode")]
    public string NavDisplayMode { get; set; } = "icon-text";
}
