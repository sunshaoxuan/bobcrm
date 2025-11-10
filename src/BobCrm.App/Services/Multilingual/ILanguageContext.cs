namespace BobCrm.App.Services.Multilingual;

/// <summary>
/// Provides access to the current user's language preferences and system language configuration.
/// </summary>
/// <remarks>
/// This interface follows the Single Responsibility Principle by focusing solely on language context,
/// separating it from text resolution logic.
/// </remarks>
public interface ILanguageContext
{
    /// <summary>
    /// Gets the current user's selected language code (e.g., "ja", "en", "zh").
    /// </summary>
    /// <value>
    /// A lowercase language code, or the system default if user preference is not available.
    /// Never returns null or empty string.
    /// </value>
    string CurrentLanguage { get; }

    /// <summary>
    /// Gets the ordered list of fallback language codes to try if the current language is unavailable.
    /// </summary>
    /// <value>
    /// An array of language codes in priority order. Never null, but may be empty.
    /// </value>
    string[] FallbackLanguages { get; }
}
