namespace BobCrm.App.Services.Multilingual;

/// <summary>
/// Resolves multilingual text dictionaries to single strings based on current user language and fallback rules.
/// </summary>
/// <remarks>
/// <para>Resolution order:</para>
/// <list type="number">
/// <item>Current user language from <see cref="ILanguageContext"/></item>
/// <item>Configured fallback languages from <see cref="MultilingualOptions"/></item>
/// <item>First non-empty value in the dictionary</item>
/// <item>Provided fallback string</item>
/// </list>
/// <para>This interface follows the Strategy pattern, allowing different resolution strategies to be plugged in.</para>
/// </remarks>
public interface IMultilingualTextResolver
{
    /// <summary>
    /// Resolves a multilingual text dictionary to a single string.
    /// </summary>
    /// <param name="multilingualText">
    /// Dictionary with language codes (e.g., "ja", "en", "zh") as keys and translated text as values.
    /// Can be null or empty.
    /// </param>
    /// <param name="fallback">
    /// Fallback string to return if no translations are available. Defaults to empty string.
    /// </param>
    /// <returns>
    /// Resolved text in the most appropriate language, or the fallback if no suitable translation exists.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fallback"/> is null.</exception>
    string Resolve(Dictionary<string, string?>? multilingualText, string fallback = "");
}
