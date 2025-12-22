using System.Text.Json;
using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Services;

public static class TemplateStateBindingRuleEngine
{
    public static int? SelectTemplateId(IEnumerable<TemplateStateBinding> bindings, JsonElement? entityData)
    {
        if (bindings == null)
        {
            return null;
        }

        var bindingList = bindings.ToList();
        if (bindingList.Count == 0)
        {
            return null;
        }

        if (entityData.HasValue)
        {
            var candidates = bindingList
                .Where(b => !string.IsNullOrWhiteSpace(b.MatchFieldName))
                .OrderByDescending(b => b.Priority)
                .ThenBy(b => b.Id)
                .ToList();

            foreach (var binding in candidates)
            {
                if (string.IsNullOrWhiteSpace(binding.MatchFieldValue))
                {
                    continue;
                }

                if (!TryGetFieldValueAsString(entityData.Value, binding.MatchFieldName!, out var actualValue))
                {
                    continue;
                }

                if (actualValue == null)
                {
                    continue;
                }

                if (string.Equals(actualValue, binding.MatchFieldValue, StringComparison.OrdinalIgnoreCase))
                {
                    return binding.TemplateId;
                }
            }
        }

        var defaultBinding = bindingList
            .Where(b => b.IsDefault)
            .OrderByDescending(b => b.Priority)
            .ThenByDescending(b => b.CreatedAt)
            .FirstOrDefault();

        if (defaultBinding != null)
        {
            return defaultBinding.TemplateId;
        }

        var genericBinding = bindingList
            .Where(b => string.IsNullOrWhiteSpace(b.MatchFieldName))
            .OrderByDescending(b => b.Priority)
            .ThenByDescending(b => b.CreatedAt)
            .FirstOrDefault();

        return genericBinding?.TemplateId;
    }

    private static bool TryGetFieldValueAsString(JsonElement entityData, string fieldName, out string? value)
    {
        value = null;

        if (string.IsNullOrWhiteSpace(fieldName) || entityData.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var property in entityData.EnumerateObject())
        {
            if (!string.Equals(property.Name, fieldName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var element = property.Value;
            value = element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Null => null,
                JsonValueKind.Undefined => null,
                _ => element.GetRawText()
            };
            return true;
        }

        return false;
    }
}

