using Microsoft.Extensions.Options;

namespace BobCrm.App.Services.Multilingual;

/// <summary>
/// Language context implementation that derives current language from <see cref="I18nService"/>.
/// </summary>
/// <remarks>
/// This adapter bridges the existing I18nService with the new ILanguageContext abstraction.
/// </remarks>
public class I18nLanguageContext : ILanguageContext
{
    private readonly I18nService _i18nService;
    private readonly MultilingualOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="I18nLanguageContext"/> class.
    /// </summary>
    /// <param name="i18nService">The I18n service that tracks current user language.</param>
    /// <param name="options">Multilingual configuration options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="i18nService"/> or <paramref name="options"/> is null.
    /// </exception>
    public I18nLanguageContext(I18nService i18nService, IOptions<MultilingualOptions> options)
    {
        _i18nService = i18nService ?? throw new ArgumentNullException(nameof(i18nService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public string CurrentLanguage
    {
        get
        {
            var lang = _i18nService.CurrentLang?.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(lang))
                return _options.DefaultLanguage;

            return lang;
        }
    }

    /// <inheritdoc/>
    public string[] FallbackLanguages => _options.FallbackLanguages.ToArray();
}
