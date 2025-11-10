namespace BobCrm.Api.Services;

/// <summary>
/// Static helper for resolving multilingual text in backend services where DI is not available.
/// </summary>
/// <remarks>
/// For backend code generation scenarios, we use a simple static helper with hardcoded
/// language fallback (ja → en → zh), as generated code (XML comments, DDL comments)
/// needs a fixed language and doesn't support dynamic user preferences.
/// </remarks>
public static class MultilingualTextHelper
{
    /// <summary>
    /// Resolves multilingual text to a single string using fixed language fallback.
    /// </summary>
    /// <param name="multilingualText">Dictionary with language codes as keys.</param>
    /// <param name="fallback">Fallback string if no translations are available.</param>
    /// <returns>Resolved text or fallback.</returns>
    public static string Resolve(Dictionary<string, string?>? multilingualText, string fallback = "")
    {
        if (multilingualText == null || multilingualText.Count == 0)
            return fallback;

        // Fixed fallback order for generated code: ja → en → zh
        // This is intentional for code generation scenarios
        if (TryGetNonEmptyValue(multilingualText, "ja", out var value))
            return value;

        if (TryGetNonEmptyValue(multilingualText, "en", out value))
            return value;

        if (TryGetNonEmptyValue(multilingualText, "zh", out value))
            return value;

        // Return first non-empty value
        var firstNonEmpty = multilingualText.Values
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        return !string.IsNullOrWhiteSpace(firstNonEmpty) ? firstNonEmpty : fallback;
    }

    private static bool TryGetNonEmptyValue(
        Dictionary<string, string?> dictionary,
        string language,
        out string value)
    {
        value = string.Empty;

        if (dictionary.TryGetValue(language, out var rawValue)
            && !string.IsNullOrWhiteSpace(rawValue))
        {
            value = rawValue;
            return true;
        }

        return false;
    }
}
