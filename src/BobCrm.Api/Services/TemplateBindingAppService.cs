using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.DTOs.Template;
using BobCrm.Api.Contracts.Responses.Template;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Utils;
using BobCrm.Api.Core.DomainCommon;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Extensions;
using Microsoft.EntityFrameworkCore;
using BobCrm.Api.Services;

namespace BobCrm.Api.Services;

public class TemplateBindingAppService : ITemplateBindingAppService
{
    private readonly AppDbContext _db;
    private readonly ILogger<TemplateBindingAppService> _logger;

    public TemplateBindingAppService(AppDbContext db, ILogger<TemplateBindingAppService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<MenuTemplateIntersectionDto>> GetMenuTemplateIntersectionsAsync(
        string uid,
        string? targetLang,
        string viewState,
        CancellationToken ct = default)
    {
        var resolvedViewState = string.IsNullOrWhiteSpace(viewState) ? "DetailView" : viewState;
        var now = DateTime.UtcNow;

        var accessibleFunctionIds = await _db.RoleAssignments
            .Where(a => a.UserId == uid &&
                        (!a.ValidFrom.HasValue || a.ValidFrom <= now) &&
                        (!a.ValidTo.HasValue || a.ValidTo >= now))
            .SelectMany(a => a.Role!.Functions)
            .Select(rf => rf.FunctionId)
            .Distinct()
            .ToListAsync(ct);

        if (accessibleFunctionIds.Count == 0)
        {
            return new List<MenuTemplateIntersectionDto>();
        }

        var menuNodes = await _db.FunctionNodes
            .AsNoTracking()
            .Include(fn => fn.TemplateStateBinding!)
            .ThenInclude(tsb => tsb.Template)
            .Where(fn => fn.TemplateStateBindingId != null && accessibleFunctionIds.Contains(fn.Id))
            .ToListAsync(ct);

        var filteredNodes = menuNodes
            .Where(fn => fn.TemplateStateBinding != null && fn.TemplateStateBinding.ViewState == resolvedViewState)
            .OrderBy(fn => fn.SortOrder)
            .ToList();

        if (filteredNodes.Count == 0)
        {
            return new List<MenuTemplateIntersectionDto>();
        }

        var entityTypes = filteredNodes
            .Select(fn => fn.TemplateStateBinding!.EntityType)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var entityTypeSet = new HashSet<string>(entityTypes, StringComparer.OrdinalIgnoreCase);

        var entityMetadata = await _db.EntityDefinitions
            .AsNoTracking()
            .Where(ed => ed.EntityRoute != null && entityTypeSet.Contains(ed.EntityRoute))
            .ToDictionaryAsync(
                ed => ed.EntityRoute!,
                ed =>
                {
                    var summary = ed.ToSummaryDto(targetLang);
                    var displayNameSingle = summary.DisplayName
                        ?? summary.DisplayNameTranslations?.Resolve(targetLang ?? string.Empty)
                        ?? ed.EntityName;
                    return (DisplayName: displayNameSingle, DisplayNameTranslations: summary.DisplayNameTranslations, Route: ResolveRoute(ed));
                },
                StringComparer.OrdinalIgnoreCase,
                ct);

        var candidateTemplates = await _db.FormTemplates
            .AsNoTracking()
            .Where(t => t.EntityType != null &&
                        entityTypeSet.Contains(t.EntityType!) &&
                        (t.UserId == uid || t.IsSystemDefault))
            .ToListAsync(ct);

        var templatesByEntity = candidateTemplates
            .GroupBy(t => t.EntityType!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var result = new List<MenuTemplateIntersectionDto>(filteredNodes.Count);
        foreach (var node in filteredNodes)
        {
            var binding = node.TemplateStateBinding!;
            var key = binding.EntityType;
            var usageType = binding.ViewState switch
            {
                "List" => FormTemplateUsageType.List,
                "DetailEdit" => FormTemplateUsageType.Edit,
                "Create" => FormTemplateUsageType.Combined,
                _ => FormTemplateUsageType.Detail
            };
            templatesByEntity.TryGetValue(key, out var templateList);
            templateList ??= new List<FormTemplate>();

            if (binding.Template != null && templateList.All(t => t.Id != binding.TemplateId))
            {
                templateList = new List<FormTemplate>(templateList) { binding.Template };
            }

            var templates = templateList
                .OrderByDescending(t => t.IsUserDefault)
                .ThenByDescending(t => t.IsSystemDefault)
                .ThenBy(t => t.Name)
                .Select(t => new TemplateSummaryDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    EntityType = t.EntityType,
                    UsageType = t.UsageType,
                    IsUserDefault = t.IsUserDefault,
                    IsSystemDefault = t.IsSystemDefault,
                    Description = t.Description,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    IsInUse = t.IsInUse
                })
                .ToList();

            entityMetadata.TryGetValue(binding.EntityType, out var metadata);
            var displayName = metadata.DisplayName ?? node.Name;
            var resolvedRoute = metadata.Route ?? node.Route;
            var displayNameTranslations = metadata.DisplayNameTranslations ??
                (node.DisplayName == null ? null : new MultilingualText(node.DisplayName));
            var resolvedMenuName = !string.IsNullOrWhiteSpace(targetLang)
                ? (displayNameTranslations?.Resolve(targetLang) ?? displayName ?? node.Name)
                : null;

            var entry = new MenuTemplateIntersectionDto
            {
                Menu = new MenuNodeSummaryDto
                {
                    Id = node.Id,
                    Code = NormalizeMenuCode(node.Code),
                    Name = resolvedMenuName ?? displayName ?? node.Name ?? string.Empty,
                    DisplayNameKey = node.DisplayNameKey,
                    DisplayName = string.IsNullOrWhiteSpace(targetLang) ? null : (resolvedMenuName ?? displayName),
                    DisplayNameTranslations = string.IsNullOrWhiteSpace(targetLang) ? displayNameTranslations : null,
                    Route = resolvedRoute,
                    Icon = node.Icon,
                    SortOrder = node.SortOrder
                },
                Binding = new TemplateStateBindingSummaryDto
                {
                    Id = binding.Id,
                    EntityType = binding.EntityType,
                    ViewState = binding.ViewState,
                    UsageType = usageType,
                    TemplateId = binding.TemplateId,
                    IsDefault = binding.IsDefault,
                    RequiredPermission = binding.RequiredPermission
                },
                Templates = templates
            };

            result.Add(entry);
        }

        _logger.LogDebug("[Templates] Calculated {Count} menu/template intersections for user {UserId}.", result.Count, uid);
        return result;
    }

    private static string NormalizeMenuCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        if (code.EndsWith(".DETAIL", StringComparison.OrdinalIgnoreCase) ||
            code.EndsWith(".EDIT", StringComparison.OrdinalIgnoreCase))
        {
            var index = code.LastIndexOf('.');
            return index > 0 ? code[..index] : code;
        }

        return code;
    }

    private static string? ResolveRoute(EntityDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.ApiEndpoint))
        {
            return null;
        }

        var route = definition.ApiEndpoint;
        if (route.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            route = route[4..];
        }
        return route;
    }
}
