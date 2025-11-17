using System.Globalization;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BobCrm.Api.Services;

/// <summary>
/// 注册实体对应的菜单节点，并将模板绑定与菜单功能码关联。
/// </summary>
public class EntityMenuRegistrar
{
    private readonly AppDbContext _db;
    private readonly ILogger<EntityMenuRegistrar> _logger;

    private static readonly Dictionary<string, string> DomainCodeMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["System"] = "SYS",
        ["SYS"] = "SYS",
        ["Custom"] = "CUSTOM"
    };

    private static readonly Dictionary<string, string> DomainIcons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SYS"] = "setting",
        ["CRM"] = "team",
        ["SCM"] = "branches",
        ["FA"] = "dollar",
        ["HR"] = "user",
        ["MFM"] = "build",
        ["CUSTOM"] = "appstore"
    };

    private const string RootFunctionCode = "APP.ROOT";

    public EntityMenuRegistrar(AppDbContext db, ILogger<EntityMenuRegistrar> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<EntityMenuRegistrationResult> RegisterAsync(
        EntityDefinition entity,
        string? publishedBy = null,
        CancellationToken ct = default)
    {
        var result = new EntityMenuRegistrationResult
        {
            DomainCode = ResolveDomainCode(entity.Category)
        };

        try
        {
            var rootNode = await EnsureRootNodeAsync(ct);
            var domainNode = await EnsureDomainNodeAsync(entity, rootNode, result.DomainCode, ct);
            var moduleNode = await EnsureModuleNodeAsync(domainNode, ct);
            var entityNode = await EnsureEntityNodeAsync(entity, moduleNode, ct);
            var binding = await EnsureTemplateBindingAsync(entity, entityNode.Code, publishedBy, ct);

            if (_db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
            {
                await using var transaction = await _db.Database.BeginTransactionAsync(ct);
                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            else
            {
                await _db.SaveChangesAsync(ct);
            }

            result.Success = true;
            result.DomainNodeId = domainNode.Id;
            result.ModuleNodeId = moduleNode.Id;
            result.FunctionNodeId = entityNode.Id;
            result.FunctionCode = entityNode.Code;
            result.TemplateBindingId = binding?.Id;

            if (binding == null)
            {
                result.Warning = "Template binding not found";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MenuRegistrar] Failed to register menu for entity {EntityName}", entity.EntityName);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _db.ChangeTracker.Clear();
        }

        return result;
    }

    private async Task<FunctionNode> EnsureRootNodeAsync(CancellationToken ct)
    {
        var root = await _db.FunctionNodes.FirstOrDefaultAsync(f => f.Code == RootFunctionCode, ct);
        if (root != null)
        {
            return root;
        }

        root = new FunctionNode
        {
            Code = RootFunctionCode,
            Name = "应用根节点",
            Icon = "appstore",
            IsMenu = true,
            SortOrder = 0
        };
        _db.FunctionNodes.Add(root);
        return root;
    }

    private async Task<FunctionNode> EnsureDomainNodeAsync(
        EntityDefinition entity,
        FunctionNode rootNode,
        string normalizedDomainCode,
        CancellationToken ct)
    {
        var domainNode = await _db.FunctionNodes.FirstOrDefaultAsync(
            f => f.Code == normalizedDomainCode,
            ct);

        if (domainNode != null)
        {
            if (domainNode.ParentId != rootNode.Id)
            {
                domainNode.ParentId = rootNode.Id;
            }
            return domainNode;
        }

        var domainInfo = await ResolveDomainInfoAsync(entity.Category, normalizedDomainCode, ct);
        var icon = ResolveDomainIcon(normalizedDomainCode);

        domainNode = new FunctionNode
        {
            ParentId = rootNode.Id,
            Code = normalizedDomainCode,
            Name = domainInfo.Name,
            Icon = icon,
            IsMenu = true,
            SortOrder = domainInfo.SortOrder
        };

        _db.FunctionNodes.Add(domainNode);
        return domainNode;
    }

    private async Task<FunctionNode> EnsureModuleNodeAsync(FunctionNode domainNode, CancellationToken ct)
    {
        var moduleCode = $"{domainNode.Code}.ENTITY";
        var moduleNode = await _db.FunctionNodes.FirstOrDefaultAsync(f => f.Code == moduleCode, ct);
        if (moduleNode != null)
        {
            if (moduleNode.ParentId != domainNode.Id)
            {
                moduleNode.ParentId = domainNode.Id;
            }
            if (!string.Equals(moduleNode.Name, "业务实体", StringComparison.Ordinal))
            {
                moduleNode.Name = "业务实体";
            }
            if (moduleNode.SortOrder == 0)
            {
                moduleNode.SortOrder = domainNode.SortOrder + 1;
            }
            if (moduleNode.Icon != "database")
            {
                moduleNode.Icon = "database";
            }
            return moduleNode;
        }

        moduleNode = new FunctionNode
        {
            ParentId = domainNode.Id,
            Code = moduleCode,
            Name = "业务实体",
            Icon = "database",
            IsMenu = true,
            SortOrder = domainNode.SortOrder + 1
        };
        _db.FunctionNodes.Add(moduleNode);
        return moduleNode;
    }

    private async Task<FunctionNode> EnsureEntityNodeAsync(
        EntityDefinition entity,
        FunctionNode moduleNode,
        CancellationToken ct)
    {
        var entityCodeSegment = NormalizeCodeSegment(entity.EntityRoute ?? entity.EntityName);
        var entityFunctionCode = $"{moduleNode.Code}.{entityCodeSegment}";
        var entityNode = await _db.FunctionNodes.FirstOrDefaultAsync(f => f.Code == entityFunctionCode, ct);

        var displayName = ResolveEntityDisplayName(entity);
        var route = $"/dynamic-entity/{entity.FullTypeName}";
        var icon = string.IsNullOrWhiteSpace(entity.Icon) ? "appstore" : entity.Icon;
        var sortOrder = entity.Order == 0 ? moduleNode.SortOrder + 10 : entity.Order;

        if (entityNode != null)
        {
            entityNode.ParentId = moduleNode.Id;
            entityNode.Name = displayName;
            entityNode.Route = route;
            entityNode.Icon = icon;
            entityNode.IsMenu = true;
            entityNode.SortOrder = sortOrder;
            return entityNode;
        }

        entityNode = new FunctionNode
        {
            ParentId = moduleNode.Id,
            Code = entityFunctionCode,
            Name = displayName,
            Route = route,
            Icon = icon,
            IsMenu = true,
            SortOrder = sortOrder
        };
        _db.FunctionNodes.Add(entityNode);
        return entityNode;
    }

    private async Task<TemplateBinding?> EnsureTemplateBindingAsync(
        EntityDefinition entity,
        string functionCode,
        string? publishedBy,
        CancellationToken ct)
    {
        var binding = await FindTemplateBindingAsync(entity, ct);
        if (binding == null)
        {
            _logger.LogWarning(
                "[MenuRegistrar] Template binding not found for entity {EntityType}",
                entity.EntityRoute ?? entity.FullTypeName);
            return null;
        }

        if (!string.Equals(binding.RequiredFunctionCode, functionCode, StringComparison.Ordinal))
        {
            binding.RequiredFunctionCode = functionCode;
        }
        binding.UpdatedAt = DateTime.UtcNow;
        binding.UpdatedBy = string.IsNullOrWhiteSpace(publishedBy) ? "system" : publishedBy;
        return binding;
    }

    private async Task<TemplateBinding?> FindTemplateBindingAsync(EntityDefinition entity, CancellationToken ct)
    {
        var candidates = new List<string?>
        {
            entity.EntityRoute,
            entity.FullTypeName,
            entity.EntityName
        };

        foreach (var candidate in candidates.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var binding = await _db.TemplateBindings
                .FirstOrDefaultAsync(
                    b => b.EntityType == candidate && b.UsageType == FormTemplateUsageType.Detail,
                    ct);
            if (binding != null)
            {
                return binding;
            }
        }

        return null;
    }

    private async Task<DomainInfo> ResolveDomainInfoAsync(string? category, string normalizedDomainCode, CancellationToken ct)
    {
        var domain = await _db.EntityDomains
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Code == category, ct)
            ?? await _db.EntityDomains.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Code == normalizedDomainCode, ct);

        if (domain == null)
        {
            return new DomainInfo
            {
                Code = normalizedDomainCode,
                Name = normalizedDomainCode,
                SortOrder = 100
            };
        }

        return new DomainInfo
        {
            Code = domain.Code,
            Name = ResolveDomainDisplayName(domain),
            SortOrder = domain.SortOrder == 0 ? 100 : domain.SortOrder
        };
    }

    private static string ResolveDomainDisplayName(EntityDomain domain)
    {
        if (domain.Name == null || domain.Name.Count == 0)
        {
            return domain.Code;
        }

        foreach (var key in new[] { "zh", "ja", "en" })
        {
            if (domain.Name.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value!;
            }
        }

        return domain.Name.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? domain.Code;
    }

    private static string ResolveDomainCode(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return "CUSTOM";
        }

        if (DomainCodeMappings.TryGetValue(category, out var mapped))
        {
            return mapped.ToUpperInvariant();
        }

        return category.ToUpperInvariant();
    }

    private static string ResolveDomainIcon(string normalizedDomainCode)
    {
        if (DomainIcons.TryGetValue(normalizedDomainCode, out var icon))
        {
            return icon;
        }
        return "appstore";
    }

    private static string NormalizeCodeSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "ENTITY";
        }

        var normalized = value
            .Trim()
            .Replace('-', '_')
            .Replace(' ', '_');
        return normalized.ToUpper(CultureInfo.InvariantCulture);
    }

    private static string ResolveEntityDisplayName(EntityDefinition entity)
    {
        if (entity.DisplayName != null)
        {
            foreach (var key in new[] { "zh", "ja", "en" })
            {
                if (entity.DisplayName.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                {
                    return value!;
                }
            }

            var first = entity.DisplayName.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
            if (!string.IsNullOrWhiteSpace(first))
            {
                return first!;
            }
        }

        return entity.EntityName;
    }

    private sealed class DomainInfo
    {
        public string Code { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public int SortOrder { get; init; }
    }
}

public class EntityMenuRegistrationResult
{
    public bool Success { get; set; }
    public string DomainCode { get; set; } = string.Empty;
    public Guid? DomainNodeId { get; set; }
    public Guid? ModuleNodeId { get; set; }
    public Guid? FunctionNodeId { get; set; }
    public string? FunctionCode { get; set; }
    public int? TemplateBindingId { get; set; }
    public string? Warning { get; set; }
    public string? ErrorMessage { get; set; }
}
