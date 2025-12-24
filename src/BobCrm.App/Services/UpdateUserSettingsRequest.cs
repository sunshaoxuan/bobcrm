using System.Text.Json.Serialization;

namespace BobCrm.App.Services;

public class UpdateUserSettingsRequest
{
    [JsonPropertyName("theme")]
    public string? Theme { get; set; }

    [JsonPropertyName("primaryColor")]
    public string? PrimaryColor { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("homeRoute")]
    public string? HomeRoute { get; set; }

    [JsonPropertyName("navDisplayMode")]
    public string? NavDisplayMode { get; set; }

    [JsonIgnore]
    public bool IsEmpty =>
        Theme is null &&
        PrimaryColor is null &&
        Language is null &&
        HomeRoute is null &&
        NavDisplayMode is null;
}
