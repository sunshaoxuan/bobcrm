using BobCrm.Api.Infrastructure;

namespace BobCrm.Api.Base;

public static class SystemEntityI18n
{
    private static readonly Lazy<Dictionary<string, Dictionary<string, string?>>> ResourceCache =
        new(() => I18nResourceLoader.LoadResources()
            .ToDictionary(
                r => r.Key,
                r => r.Translations.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (string?)kvp.Value,
                    StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase));

    public static Dictionary<string, string?> Dict(string key)
    {
        if (ResourceCache.Value.TryGetValue(key, out var translations) && translations.Count > 0)
        {
            return new Dictionary<string, string?>(translations, StringComparer.OrdinalIgnoreCase);
        }

        return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = key
        };
    }
}

