namespace BobCrm.App.Services.Multilingual;

/// <summary>
/// Default implementation of <see cref="IMultilingualTextResolver"/> that resolves text based on
/// current language context and configured fallback rules.
/// </summary>
public class MultilingualTextResolver : IMultilingualTextResolver
{
    private readonly ILanguageContext _languageContext;
    private readonly ILogger<MultilingualTextResolver> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultilingualTextResolver"/> class.
    /// </summary>
    /// <param name="languageContext">The language context providing current and fallback languages.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="languageContext"/> or <paramref name="logger"/> is null.
    /// </exception>
    public MultilingualTextResolver(
        ILanguageContext languageContext,
        ILogger<MultilingualTextResolver> logger)
    {
        _languageContext = languageContext ?? throw new ArgumentNullException(nameof(languageContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public string Resolve(Dictionary<string, string?>? multilingualText, string fallback = "")
    {
        ArgumentNullException.ThrowIfNull(fallback);

        // Guard: Empty input
        if (multilingualText == null || multilingualText.Count == 0)
        {
            _logger.LogDebug("Empty multilingual text provided, returning fallback: '{Fallback}'", fallback);
            return fallback;
        }

        // Step 1: Try current language
        var currentLang = _languageContext.CurrentLanguage;
        if (TryGetNonEmptyValue(multilingualText, currentLang, out var value))
        {
            _logger.LogTrace(
                "Resolved text using current language '{Language}': '{Value}'",
                currentLang,
                value);
            return value;
        }

        // Step 2: Try fallback languages in order
        foreach (var fallbackLang in _languageContext.FallbackLanguages)
        {
            if (fallbackLang == currentLang)
                continue; // Skip if same as current

            if (TryGetNonEmptyValue(multilingualText, fallbackLang, out value))
            {
                _logger.LogDebug(
                    "Resolved text using fallback language '{FallbackLanguage}' (current: '{CurrentLanguage}'): '{Value}'",
                    fallbackLang,
                    currentLang,
                    value);
                return value;
            }
        }

        // Step 3: Try first non-empty value
        var firstNonEmpty = multilingualText.Values
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        if (!string.IsNullOrWhiteSpace(firstNonEmpty))
        {
            _logger.LogDebug(
                "No matching language found, using first non-empty value: '{Value}'",
                firstNonEmpty);
            return firstNonEmpty;
        }

        // Step 4: Return fallback
        _logger.LogDebug(
            "All multilingual values are empty, returning fallback: '{Fallback}'",
            fallback);
        return fallback;
    }

    /// <summary>
    /// Tries to get a non-empty value from the dictionary for the specified language.
    /// </summary>
    /// <param name="dictionary">The dictionary to search.</param>
    /// <param name="language">The language code to look up (case-insensitive).</param>
    /// <param name="value">The retrieved value if found and non-empty.</param>
    /// <returns>True if a non-empty value was found; otherwise, false.</returns>
    private static bool TryGetNonEmptyValue(
        Dictionary<string, string?> dictionary,
        string language,
        out string value)
    {
        value = string.Empty;

        if (string.IsNullOrWhiteSpace(language))
            return false;

        // Case-insensitive lookup
        if (dictionary.TryGetValue(language, out var rawValue)
            && !string.IsNullOrWhiteSpace(rawValue))
        {
            value = rawValue;
            return true;
        }

        return false;
    }
}
