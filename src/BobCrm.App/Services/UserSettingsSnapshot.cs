using System.Text.Json.Serialization;

namespace BobCrm.App.Services;

public class UserSettingsSnapshot
{
    [JsonPropertyName("system")]
    public SystemSettingsDto System { get; set; } = new();

    [JsonPropertyName("effective")]
    public UserSettingsDto Effective { get; set; } = new();

    [JsonPropertyName("overrides")]
    public UserSettingsDto? Overrides { get; set; }
}
