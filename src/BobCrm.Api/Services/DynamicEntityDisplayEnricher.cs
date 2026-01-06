using System.Text.Json;
using System.Text.Json.Nodes;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.Responses.Entity;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

public sealed class DynamicEntityDisplayEnricher
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _db;
    private readonly IFieldMetadataCache _fieldMetadataCache;
    private readonly LookupResolveService _lookupResolveService;
    private readonly ILogger<DynamicEntityDisplayEnricher> _logger;

    public DynamicEntityDisplayEnricher(
        AppDbContext db,
        IFieldMetadataCache fieldMetadataCache,
        LookupResolveService lookupResolveService,
        ILogger<DynamicEntityDisplayEnricher> logger)
    {
        _db = db;
        _fieldMetadataCache = fieldMetadataCache;
        _lookupResolveService = lookupResolveService;
        _logger = logger;
    }

    public async Task<List<object>> EnrichListAsync(
        string fullTypeName,
        IReadOnlyList<object> entities,
        ILocalization loc,
        string lang,
        CancellationToken ct)
    {
        if (entities.Count == 0)
        {
            return new List<object>();
        }

        var fields = await _fieldMetadataCache.GetFieldsAsync(fullTypeName, loc, lang, ct);
        if (fields.Count == 0)
        {
            return entities.ToList();
        }

        var enumDisplay = await LoadEnumDisplayMapsAsync(fields, lang, ct);
        var lookupDisplay = await LoadLookupDisplayMapsAsync(fields, entities, ct);

        var result = new List<object>(entities.Count);
        foreach (var entity in entities)
        {
            result.Add(EnrichEntity(entity, fields, enumDisplay, lookupDisplay));
        }

        return result;
    }

    public async Task<object> EnrichSingleAsync(
        string fullTypeName,
        object entity,
        ILocalization loc,
        string lang,
        CancellationToken ct)
    {
        var fields = await _fieldMetadataCache.GetFieldsAsync(fullTypeName, loc, lang, ct);
        if (fields.Count == 0)
        {
            return entity;
        }

        var enumDisplay = await LoadEnumDisplayMapsAsync(fields, lang, ct);
        var lookupDisplay = await LoadLookupDisplayMapsAsync(fields, [entity], ct);
        return EnrichEntity(entity, fields, enumDisplay, lookupDisplay);
    }

    private static JsonObject EnrichEntity(
        object entity,
        IReadOnlyList<FieldMetadataDto> fields,
        Dictionary<Guid, Dictionary<string, string>> enumDisplayByDefinition,
        Dictionary<string, Dictionary<string, string>> lookupDisplayByTarget)
    {
        var node = JsonSerializer.SerializeToNode(entity, WebJson) as JsonObject ?? new JsonObject();
        var display = new JsonObject();

        foreach (var field in fields)
        {
            if (string.IsNullOrWhiteSpace(field.PropertyName))
            {
                continue;
            }

            if (field.EnumDefinitionId.HasValue &&
                string.Equals(field.DataType, FieldDataType.Enum, StringComparison.OrdinalIgnoreCase))
            {
                if (TryGetValueNode(node, field.PropertyName, out var valueNode, out var jsonKey))
                {
                    var rendered = RenderEnumDisplay(valueNode, field, enumDisplayByDefinition);
                    if (!string.IsNullOrWhiteSpace(rendered))
                    {
                        display[jsonKey] = rendered;
                    }
                }

                continue;
            }

            if (field.IsEntityRef && (!string.IsNullOrWhiteSpace(field.LookupEntityName) || field.ReferencedEntityId.HasValue))
            {
                if (TryGetValueNode(node, field.PropertyName, out var valueNode, out var jsonKey))
                {
                    var key = ExtractLookupKey(valueNode);
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        var targetKey = BuildLookupTargetKey(field);
                        if (lookupDisplayByTarget.TryGetValue(targetKey, out var map) &&
                            map.TryGetValue(key, out var label) &&
                            !string.IsNullOrWhiteSpace(label))
                        {
                            display[jsonKey] = label;
                        }
                    }
                }
            }
        }

        if (display.Count > 0)
        {
            node["__display"] = display;
        }

        return node;
    }

    private static string BuildLookupTargetKey(FieldMetadataDto field)
    {
        var target = field.LookupEntityName?.Trim();
        if (!string.IsNullOrWhiteSpace(target))
        {
            return $"name:{target}";
        }

        if (field.ReferencedEntityId.HasValue)
        {
            return $"id:{field.ReferencedEntityId.Value:D}";
        }

        return "unknown";
    }

    private static bool TryGetValueNode(JsonObject obj, string propertyName, out JsonNode? value, out string jsonKey)
    {
        value = null;
        jsonKey = propertyName;

        if (obj.TryGetPropertyValue(propertyName, out value))
        {
            jsonKey = propertyName;
            return true;
        }

        var camel = ToCamelCase(propertyName);
        if (!string.Equals(camel, propertyName, StringComparison.Ordinal) && obj.TryGetPropertyValue(camel, out value))
        {
            jsonKey = camel;
            return true;
        }

        foreach (var kv in obj)
        {
            if (string.Equals(kv.Key, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                jsonKey = kv.Key;
                value = kv.Value;
                return true;
            }
        }

        return false;
    }

    private static string ToCamelCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        if (input.Length == 1)
        {
            return input.ToLowerInvariant();
        }

        return char.ToLowerInvariant(input[0]) + input[1..];
    }

    private static string? ExtractLookupKey(JsonNode? valueNode)
    {
        if (valueNode is null)
        {
            return null;
        }

        if (valueNode is JsonValue v)
        {
            return v.ToString();
        }

        if (valueNode is JsonObject obj)
        {
            if (obj.TryGetPropertyValue("id", out var id) || obj.TryGetPropertyValue("Id", out id))
            {
                return id?.ToString();
            }
        }

        return valueNode.ToString();
    }

    private static string? RenderEnumDisplay(
        JsonNode? valueNode,
        FieldMetadataDto field,
        Dictionary<Guid, Dictionary<string, string>> enumDisplayByDefinition)
    {
        if (valueNode is null || !field.EnumDefinitionId.HasValue)
        {
            return null;
        }

        if (!enumDisplayByDefinition.TryGetValue(field.EnumDefinitionId.Value, out var map))
        {
            return null;
        }

        if (valueNode is JsonArray arr)
        {
            var rendered = arr.Select(n => MapEnumValue(map, n?.ToString())).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            return rendered.Count == 0 ? null : string.Join(", ", rendered);
        }

        if (valueNode is JsonValue val)
        {
            var raw = val.ToString();
            if (field.IsMultiSelect && !string.IsNullOrWhiteSpace(raw))
            {
                var parts = SplitMultiValues(raw);
                var rendered = parts.Select(p => MapEnumValue(map, p)).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                return rendered.Count == 0 ? null : string.Join(", ", rendered);
            }

            return MapEnumValue(map, raw);
        }

        return null;
    }

    private static IReadOnlyList<string> SplitMultiValues(string raw)
    {
        var values = new List<string>();
        var trimmed = raw.Trim();
        if (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            var v = item.GetString();
                            if (!string.IsNullOrWhiteSpace(v))
                            {
                                values.Add(v!);
                            }
                        }
                    }
                    return values;
                }
            }
            catch
            {
                // fallback below
            }
        }

        foreach (var part in trimmed.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrWhiteSpace(part))
            {
                values.Add(part);
            }
        }

        return values;
    }

    private static string? MapEnumValue(Dictionary<string, string> map, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var key = raw.Trim();
        return map.TryGetValue(key, out var display) ? display : key;
    }

    private async Task<Dictionary<Guid, Dictionary<string, string>>> LoadEnumDisplayMapsAsync(
        IReadOnlyList<FieldMetadataDto> fields,
        string lang,
        CancellationToken ct)
    {
        var enumIds = fields
            .Where(f => f.EnumDefinitionId.HasValue && string.Equals(f.DataType, FieldDataType.Enum, StringComparison.OrdinalIgnoreCase))
            .Select(f => f.EnumDefinitionId!.Value)
            .Distinct()
            .ToList();

        if (enumIds.Count == 0)
        {
            return new Dictionary<Guid, Dictionary<string, string>>();
        }

        var defs = await _db.EnumDefinitions
            .AsNoTracking()
            .Include(d => d.Options)
            .Where(d => enumIds.Contains(d.Id))
            .ToListAsync(ct);

        var result = new Dictionary<Guid, Dictionary<string, string>>();
        foreach (var def in defs)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var opt in def.Options.Where(o => o.IsEnabled))
            {
                var display = ResolveMultilingual(opt.DisplayName, lang) ?? opt.Value;
                map[opt.Value] = display;
            }

            result[def.Id] = map;
        }

        return result;
    }

    private async Task<Dictionary<string, Dictionary<string, string>>> LoadLookupDisplayMapsAsync(
        IReadOnlyList<FieldMetadataDto> fields,
        IReadOnlyList<object> entities,
        CancellationToken ct)
    {
        var lookupFields = fields
            .Where(f => f.IsEntityRef && (!string.IsNullOrWhiteSpace(f.LookupEntityName) || f.ReferencedEntityId.HasValue))
            .ToList();

        if (lookupFields.Count == 0)
        {
            return new Dictionary<string, Dictionary<string, string>>();
        }

        var idsByTarget = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var displayFieldByTarget = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var entity in entities)
        {
            var node = JsonSerializer.SerializeToNode(entity, WebJson) as JsonObject;
            if (node == null)
            {
                continue;
            }

            foreach (var field in lookupFields)
            {
                if (!TryGetValueNode(node, field.PropertyName, out var valueNode, out _))
                {
                    continue;
                }

                var id = ExtractLookupKey(valueNode);
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                var targetKey = BuildLookupTargetKey(field);
                if (!idsByTarget.TryGetValue(targetKey, out var set))
                {
                    set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    idsByTarget[targetKey] = set;
                    displayFieldByTarget[targetKey] = field.LookupDisplayField;
                }

                set.Add(id!);
            }
        }

        if (idsByTarget.Count == 0)
        {
            return new Dictionary<string, Dictionary<string, string>>();
        }

        var resolvedTypeNameById = new Dictionary<Guid, string>();
        foreach (var field in lookupFields)
        {
            if (field.ReferencedEntityId.HasValue && !resolvedTypeNameById.ContainsKey(field.ReferencedEntityId.Value))
            {
                var match = await _db.EntityDefinitions
                    .AsNoTracking()
                    .Where(e => e.Id == field.ReferencedEntityId.Value)
                    .Select(e => e.FullTypeName)
                    .FirstOrDefaultAsync(ct);

                if (!string.IsNullOrWhiteSpace(match))
                {
                    resolvedTypeNameById[field.ReferencedEntityId.Value] = match;
                }
            }
        }

        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (targetKey, ids) in idsByTarget)
        {
            if (ids.Count == 0)
            {
                continue;
            }

            try
            {
                string target;
                if (targetKey.StartsWith("name:", StringComparison.OrdinalIgnoreCase))
                {
                    target = targetKey["name:".Length..];
                }
                else if (targetKey.StartsWith("id:", StringComparison.OrdinalIgnoreCase) &&
                         Guid.TryParse(targetKey["id:".Length..], out var id) &&
                         resolvedTypeNameById.TryGetValue(id, out var fullType))
                {
                    target = fullType;
                }
                else
                {
                    continue;
                }

                var map = await _lookupResolveService.ResolveAsync(
                    target,
                    ids,
                    displayFieldByTarget.TryGetValue(targetKey, out var displayField) ? displayField : null,
                    ct);
                result[targetKey] = map;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[DynamicEntityDisplay] Lookup resolve failed for {TargetKey}", targetKey);
            }
        }

        return result;
    }

    private static string? ResolveMultilingual(Dictionary<string, string?>? translations, string lang)
    {
        if (translations == null || translations.Count == 0)
        {
            return null;
        }

        var normalized = (lang ?? string.Empty).Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(normalized) &&
            translations.TryGetValue(normalized, out var hit) &&
            !string.IsNullOrWhiteSpace(hit))
        {
            return hit;
        }

        if (translations.TryGetValue("en", out var en) && !string.IsNullOrWhiteSpace(en))
        {
            return en;
        }

        return translations.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
    }
}
