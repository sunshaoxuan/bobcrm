using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.DTOs.Access;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Utils;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

/// <summary>
/// Helper service that assembles function tree DTOs enriched with multilingual
/// names and template binding options.
/// </summary>
public class FunctionTreeBuilder
{
    private readonly AppDbContext _db;
    private readonly MultilingualFieldService _multilingual;

    public FunctionTreeBuilder(AppDbContext db, MultilingualFieldService multilingual)
    {
        _db = db;
        _multilingual = multilingual;
    }

    public async Task<List<FunctionNodeDto>> BuildAsync(
        IReadOnlyCollection<FunctionNode> nodes,
        string? lang = null,
        CancellationToken ct = default)
    {
        if (nodes == null)
        {
            throw new ArgumentNullException(nameof(nodes));
        }

        if (nodes.Count == 0)
        {
            return new List<FunctionNodeDto>();
        }

        var normalizedLang = string.IsNullOrWhiteSpace(lang)
            ? null
            : lang.Trim().ToLowerInvariant();

        var localizedNames = await LoadLocalizedNamesAsync(nodes, ct);
        var templateMetadata = await LoadTemplateMetadataAsync(nodes, ct);

        var dtoLookup = nodes.ToDictionary(
            n => n.Id,
            n => CreateDto(n, localizedNames, templateMetadata, normalizedLang));
        var parentMap = nodes.ToDictionary(n => n.Id, n => n.ParentId);

        List<FunctionNodeDto> roots = new();
        foreach (var source in nodes.OrderBy(n => n.SortOrder))
        {
            if (!dtoLookup.TryGetValue(source.Id, out var dto))
            {
                continue;
            }

            if (dto.ParentId.HasValue && dtoLookup.TryGetValue(dto.ParentId.Value, out var parent))
            {
                if (CreatesCycle(dto.Id, dto.ParentId.Value, parentMap))
                {
                    var dtoWithoutParent = dto with { ParentId = null };
                    roots.Add(dtoWithoutParent);
                    continue;
                }

                parent.Children.Add(dto);
            }
            else
            {
                roots.Add(dto);
            }
        }

        SortChildren(roots);
        return roots;
    }

    private async Task<Dictionary<Guid, MultilingualText?>> LoadLocalizedNamesAsync(
        IReadOnlyCollection<FunctionNode> nodes,
        CancellationToken ct)
    {
        var keySet = nodes
            .Where(n => !string.IsNullOrWhiteSpace(n.DisplayNameKey))
            .Select(n => n.DisplayNameKey!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var resourceMap = await _multilingual.LoadResourcesAsync(keySet, ct);

        var result = new Dictionary<Guid, MultilingualText?>();
        foreach (var node in nodes)
        {
            var fallback = node.DisplayName is { Count: > 0 }
                ? CloneDictionary(node.DisplayName)
                : null;

            Dictionary<string, string?>? merged = null;

            if (!string.IsNullOrWhiteSpace(node.DisplayNameKey) &&
                resourceMap.TryGetValue(node.DisplayNameKey!, out var resourceNames))
            {
                merged = _multilingual.Merge(resourceNames, fallback);
            }
            else
            {
                merged = fallback;
            }

            result[node.Id] = merged == null ? null : new MultilingualText(merged);
        }

        return result;
    }

    private async Task<Dictionary<Guid, TemplateMetadata>> LoadTemplateMetadataAsync(
        IReadOnlyCollection<FunctionNode> nodes,
        CancellationToken ct)
    {
        var codeSet = nodes
            .Where(n => !string.IsNullOrWhiteSpace(n.Code))
            .Select(n => n.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (codeSet.Count == 0)
        {
            return new Dictionary<Guid, TemplateMetadata>();
        }

        var bindings = await _db.TemplateStateBindings
            .AsNoTracking()
            .Include(b => b.Template)
            .Where(b => b.RequiredPermission != null && codeSet.Contains(b.RequiredPermission))
            .ToListAsync(ct);

        var bindingMap = bindings
            .GroupBy(b => b.RequiredPermission!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<Guid, TemplateMetadata>();
        foreach (var node in nodes)
        {
            if (!bindingMap.TryGetValue(node.Code, out var bindingList))
            {
                continue;
            }

            var options = bindingList
                .Select(binding => new FunctionTemplateOptionDto
                {
                    BindingId = binding.Id,
                    TemplateId = binding.TemplateId,
                    TemplateName = binding.Template?.Name ?? string.Empty,
                    EntityType = binding.EntityType,
                    UsageType = MapViewStateToUsage(binding.ViewState),
                    IsSystem = binding.IsDefault,
                    IsDefault = node.TemplateStateBindingId.HasValue && node.TemplateStateBindingId.Value == binding.Id
                })
                .OrderByDescending(o => o.IsDefault)
                .ThenBy(o => o.TemplateName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var bindingDtos = bindingList
                .Select(b => new FunctionNodeTemplateBindingDto(
                    b.Id,
                    b.EntityType,
                    MapViewStateToUsage(b.ViewState),
                    b.TemplateId,
                    b.Template?.Name ?? string.Empty,
                    b.IsDefault))
                .ToList();

            result[node.Id] = new TemplateMetadata(options, bindingDtos);
        }

        return result;
    }

    private static Dictionary<string, string?> CloneDictionary(Dictionary<string, string?> source)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (lang, text) in source)
        {
            if (string.IsNullOrWhiteSpace(lang))
            {
                continue;
            }

            var key = lang.Trim().ToLowerInvariant();
            result[key] = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        return result;
    }

    private static FunctionNodeDto CreateDto(
        FunctionNode node,
        IReadOnlyDictionary<Guid, MultilingualText?> localizedNames,
        IReadOnlyDictionary<Guid, TemplateMetadata> templateMetadata,
        string? lang)
    {
        var (displayName, translations) = ResolveDisplayName(node, localizedNames, lang);
        templateMetadata.TryGetValue(node.Id, out var metadata);
        var defaultOption = metadata?.Options.FirstOrDefault(o => o.IsDefault);
        var selectedBinding = node.TemplateStateBindingId.HasValue
            ? metadata?.Bindings.FirstOrDefault(b => b.BindingId == node.TemplateStateBindingId.Value)
            : null;
        var templateName = selectedBinding?.TemplateName ?? defaultOption?.TemplateName;
        var templateId = selectedBinding?.TemplateId ?? defaultOption?.TemplateId;

        return new FunctionNodeDto
        {
            Id = node.Id,
            ParentId = node.ParentId,
            Code = node.Code,
            Name = node.Name,
            DisplayName = displayName,
            DisplayNameTranslations = translations,
            Route = node.Route,
            Icon = node.Icon,
            IsMenu = node.IsMenu,
            SortOrder = node.SortOrder,
            TemplateId = templateId,
            TemplateName = templateName,
            Children = new List<FunctionNodeDto>(),
            TemplateOptions = metadata?.Options ?? new List<FunctionTemplateOptionDto>(),
            TemplateBindings = metadata?.Bindings ?? new List<FunctionNodeTemplateBindingDto>()
        };
    }

    private sealed record TemplateMetadata(
        List<FunctionTemplateOptionDto> Options,
        List<FunctionNodeTemplateBindingDto> Bindings);

    private static (string? displayName, MultilingualText? translations) ResolveDisplayName(
        FunctionNode node,
        IReadOnlyDictionary<Guid, MultilingualText?> localizedNames,
        string? lang)
    {
        localizedNames.TryGetValue(node.Id, out var displayNameTranslations);

        if (!string.IsNullOrWhiteSpace(lang))
        {
            var resolved = displayNameTranslations?.Resolve(lang) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(resolved))
            {
                resolved = node.Name;
            }

            return (resolved, null);
        }

        return (null, displayNameTranslations);
    }

    private static bool CreatesCycle(Guid childId, Guid parentId, Dictionary<Guid, Guid?> parentMap)
    {
        var current = parentId;
        HashSet<Guid> visited = new() { childId };

        while (true)
        {
            if (!visited.Add(current))
            {
                return true;
            }

            if (!parentMap.TryGetValue(current, out var next) || !next.HasValue)
            {
                return false;
            }

            if (next.Value == childId)
            {
                return true;
            }

            current = next.Value;
        }
    }

    private static void SortChildren(List<FunctionNodeDto> nodes)
    {
        nodes.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
        foreach (var node in nodes)
        {
            SortChildren(node.Children);
        }
    }
    private static FormTemplateUsageType MapViewStateToUsage(string viewState) =>
        viewState switch
        {
            "List" => FormTemplateUsageType.List,
            "DetailEdit" => FormTemplateUsageType.Edit,
            "Create" => FormTemplateUsageType.Combined,
            _ => FormTemplateUsageType.Detail
        };
}
