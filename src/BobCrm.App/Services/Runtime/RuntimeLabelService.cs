using System.Text.Json;
using BobCrm.Api.Abstractions;
using BobCrm.App.Models.Widgets;
using BobCrm.App.Services;

namespace BobCrm.App.Services.Runtime;

public sealed class RuntimeLabelService
{
    private readonly II18nService _i18n;
    private readonly FieldService _fieldService;

    private readonly Dictionary<string, string> _fieldLabelMap = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> EnglishLabelMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Customer Code", "COL_CODE" },
        { "Customer Name", "COL_NAME" },
        { "Extended Data", "COL_EXT_DATA" },
        { "Version", "COL_VERSION" },
        { "ID", "COL_ID" }
    };

    private static readonly Dictionary<string, string> BaseResourceMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "code", "COL_CODE" },
        { "name", "COL_NAME" },
        { "extdata", "COL_EXT_DATA" },
        { "ext_data", "COL_EXT_DATA" },
        { "extendeddata", "COL_EXT_DATA" },
        { "version", "COL_VERSION" },
        { "id", "COL_ID" }
    };

    public RuntimeLabelService(II18nService i18n, FieldService fieldService)
    {
        _i18n = i18n;
        _fieldService = fieldService;
    }

    public async Task RefreshFieldLabelsAsync(CancellationToken ct = default)
    {
        _fieldLabelMap.Clear();

        try
        {
            var defs = await _fieldService.GetDefinitionsAsync(ct);
            foreach (var def in defs.Where(d => !string.IsNullOrWhiteSpace(d.key)))
            {
                if (!string.IsNullOrWhiteSpace(def.label))
                {
                    _fieldLabelMap[def.key] = def.label;
                }
            }
        }
        catch
        {
            // Ignore: keep only base i18n fallbacks.
        }
    }

    public string ResolveLabel(DraggableWidget widget, JsonElement? entityData)
    {
        if (!string.IsNullOrWhiteSpace(widget.Label))
        {
            var label = widget.Label!;
            var looksLikeKey = label.Any(char.IsUpper) && label.Contains('_');
            if (looksLikeKey)
            {
                var translated = _i18n.T(label);
                if (!string.Equals(translated, label, StringComparison.OrdinalIgnoreCase))
                {
                    return translated;
                }
            }

            if (EnglishLabelMap.TryGetValue(label, out var mappedKey))
            {
                var translated = _i18n.T(mappedKey);
                if (!string.Equals(translated, mappedKey, StringComparison.OrdinalIgnoreCase))
                {
                    return translated;
                }
            }

            if (!string.Equals(label, widget.DataField, StringComparison.OrdinalIgnoreCase))
            {
                return label;
            }
        }

        if (!string.IsNullOrWhiteSpace(widget.DataField) && entityData.HasValue)
        {
            if (entityData.Value.TryGetProperty("fields", out var fieldsElement))
            {
                foreach (var field in fieldsElement.EnumerateArray())
                {
                    if (field.TryGetProperty("key", out var keyProp) &&
                        string.Equals(keyProp.GetString(), widget.DataField, StringComparison.OrdinalIgnoreCase) &&
                        field.TryGetProperty("label", out var labelProp))
                    {
                        var labelText = labelProp.GetString();
                        if (!string.IsNullOrWhiteSpace(labelText))
                        {
                            return labelText!;
                        }
                    }
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(widget.DataField) &&
            _fieldLabelMap.TryGetValue(widget.DataField!, out var defLabel) &&
            !string.IsNullOrWhiteSpace(defLabel))
        {
            var looksLikeKey = defLabel.Any(char.IsUpper) && defLabel.Contains('_');
            if (looksLikeKey)
            {
                var translated = _i18n.T(defLabel);
                if (!string.Equals(translated, defLabel, StringComparison.OrdinalIgnoreCase))
                {
                    return translated;
                }
            }

            return defLabel;
        }

        if (!string.IsNullOrWhiteSpace(widget.DataField))
        {
            var key = widget.DataField!;
            var normalizedKey = key.Replace("-", "_").Replace(".", "_").ToLowerInvariant();

            if (BaseResourceMap.TryGetValue(normalizedKey, out var baseKey))
            {
                var translatedBase = _i18n.T(baseKey);
                if (!string.Equals(translatedBase, baseKey, StringComparison.OrdinalIgnoreCase))
                {
                    return translatedBase;
                }
            }

            var resourceKey = $"COL_{normalizedKey}".ToUpperInvariant();
            var translated = _i18n.T(resourceKey);
            if (!string.Equals(translated, resourceKey, StringComparison.OrdinalIgnoreCase))
            {
                return translated;
            }

            if (!string.IsNullOrWhiteSpace(widget.Label) && EnglishLabelMap.TryGetValue(widget.Label!, out var mappedKey))
            {
                var translatedLabel = _i18n.T(mappedKey);
                if (!string.Equals(translatedLabel, mappedKey, StringComparison.OrdinalIgnoreCase))
                {
                    return translatedLabel;
                }
            }

            var translatedByName = _i18n.T(key);
            if (!string.Equals(translatedByName, key, StringComparison.OrdinalIgnoreCase))
            {
                return translatedByName;
            }

            return key;
        }

        return widget.Type;
    }
}

