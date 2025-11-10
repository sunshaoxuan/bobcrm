namespace BobCrm.App.Services.Multilingual;

/// <summary>
/// Configuration options for multilingual text resolution.
/// </summary>
/// <remarks>
/// This class can be bound to appsettings.json configuration section "Multilingual".
/// </remarks>
public class MultilingualOptions
{
    /// <summary>
    /// Configuration section name for binding from appsettings.json.
    /// </summary>
    public const string SectionName = "Multilingual";

    /// <summary>
    /// Gets or sets the default language code to use when user preference is unavailable.
    /// </summary>
    /// <value>Default is "ja" (Japanese).</value>
    public string DefaultLanguage { get; set; } = "ja";

    /// <summary>
    /// Gets or sets the ordered list of fallback languages to try when the current/default language is unavailable.
    /// </summary>
    /// <value>Default is ["en", "zh"] (English, then Chinese).</value>
    public List<string> FallbackLanguages { get; set; } = new() { "en", "zh" };

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    /// <returns>True if configuration is valid, otherwise false.</returns>
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(DefaultLanguage))
            return false;

        if (FallbackLanguages == null)
            return false;

        if (FallbackLanguages.Any(string.IsNullOrWhiteSpace))
            return false;

        return true;
    }
}
