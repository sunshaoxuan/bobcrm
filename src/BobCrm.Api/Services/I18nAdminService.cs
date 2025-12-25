using BobCrm.Api.Base;
using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.Requests.I18n;
using BobCrm.Api.Contracts.Responses.System;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

public sealed class I18nAdminService
{
    private static readonly string[] DefaultLanguages = ["ja", "en", "zh"];
    private static readonly string[] ProtectedPrefixes = ["MENU_", "LBL_", "BTN_", "MSG_", "ERR_", "TXT_", "VAL_", "DOC_"];

    private readonly AppDbContext _db;
    private readonly ILocalization _localization;

    public I18nAdminService(AppDbContext db, ILocalization localization)
    {
        _db = db;
        _localization = localization;
    }

    public async Task<PagedResponse<I18nResourceEntryDto>> SearchAsync(
        int page,
        int pageSize,
        string? keyQuery,
        string? culture,
        CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _db.LocalizationResources.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyQuery))
        {
            var q = keyQuery.Trim();
            query = query.Where(x => x.Key.Contains(q));
        }

        var list = await query
            .OrderBy(x => x.Key)
            .ToListAsync(ct);

        var languages = await GetLanguagesAsync(ct);
        var normalizedCulture = NormalizeCulture(culture);

        var entries = new List<I18nResourceEntryDto>(Math.Max(64, list.Count));
        foreach (var resource in list)
        {
            var isProtected = IsProtectedKey(resource.Key);
            if (!string.IsNullOrWhiteSpace(normalizedCulture))
            {
                entries.Add(new I18nResourceEntryDto
                {
                    Key = resource.Key,
                    Culture = normalizedCulture,
                    Value = resource.Translations.TryGetValue(normalizedCulture, out var v) ? v : string.Empty,
                    IsProtectedKey = isProtected
                });
                continue;
            }

            foreach (var lang in languages)
            {
                entries.Add(new I18nResourceEntryDto
                {
                    Key = resource.Key,
                    Culture = lang,
                    Value = resource.Translations.TryGetValue(lang, out var v) ? v : string.Empty,
                    IsProtectedKey = isProtected
                });
            }
        }

        var totalCount = entries.Count;
        var pageItems = entries
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResponse<I18nResourceEntryDto>(pageItems, page, pageSize, totalCount);
    }

    public async Task SaveAsync(SaveI18nResourceRequest request, CancellationToken ct)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var key = request.Key?.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Key is required");
        }

        var culture = NormalizeCulture(request.Culture);
        if (string.IsNullOrWhiteSpace(culture))
        {
            throw new InvalidOperationException("Culture is required");
        }

        if (IsProtectedKey(key) && !request.Force)
        {
            throw new InvalidOperationException("I18N_KEY_PROTECTED");
        }

        var value = request.Value ?? string.Empty;

        var entity = await _db.LocalizationResources.FirstOrDefaultAsync(x => x.Key == key, ct);
        if (entity == null)
        {
            entity = new LocalizationResource
            {
                Key = key,
                Translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };
            _db.LocalizationResources.Add(entity);
        }

        var updatedTranslations = entity.Translations.Count == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(entity.Translations, StringComparer.OrdinalIgnoreCase);

        updatedTranslations[culture] = value;
        entity.Translations = updatedTranslations;
        _db.Entry(entity).Property(x => x.Translations).IsModified = true;
        await _db.SaveChangesAsync(ct);

        _localization.InvalidateCache();
    }

    public void ReloadCache()
    {
        _localization.InvalidateCache();
    }

    public static bool IsProtectedKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        foreach (var p in ProtectedPrefixes)
        {
            if (key.StartsWith(p, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<List<string>> GetLanguagesAsync(CancellationToken ct)
    {
        var langs = await _db.LocalizationLanguages.AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => x.Code.ToLower())
            .ToListAsync(ct);

        if (langs.Count == 0)
        {
            return DefaultLanguages.ToList();
        }

        return langs;
    }

    private static string? NormalizeCulture(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return null;
        }

        var c = culture.Trim().ToLowerInvariant();
        var idx = c.IndexOf('-', StringComparison.Ordinal);
        return idx > 0 ? c[..idx] : c;
    }
}
