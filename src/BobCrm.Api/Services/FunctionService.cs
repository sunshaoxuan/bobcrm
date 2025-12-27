using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.DTOs.Access;
using BobCrm.Api.Contracts.Requests.Access;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;


namespace BobCrm.Api.Services;

public class FunctionService
{
    private readonly AppDbContext _db;
    private readonly MultilingualFieldService _multilingual;

    public FunctionService(AppDbContext db, MultilingualFieldService multilingual)
    {
        _db = db;
        _multilingual = multilingual;
    }

    public async Task<FunctionNode> CreateFunctionAsync(CreateFunctionRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new InvalidOperationException("Function code is required.");
        }

        var exists = await _db.FunctionNodes.AnyAsync(f => f.Code == request.Code, ct);
        if (exists)
        {
            throw new InvalidOperationException("Function code already exists.");
        }

        var displayName = request.DisplayName != null
            ? new Dictionary<string, string?>(request.DisplayName, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            displayName.TryAdd("zh", request.Name.Trim());
        }

        var resolvedName = !string.IsNullOrWhiteSpace(request.Name)
            ? request.Name.Trim()
            : MultilingualTextHelper.Resolve(displayName, request.Code);

        if (string.IsNullOrWhiteSpace(resolvedName))
        {
            throw new InvalidOperationException("Function name is required.");
        }

        int? stateBindingId = null;
        var requestedBindingId = request.TemplateStateBindingId ?? request.TemplateId;
        if (requestedBindingId.HasValue)
        {
            stateBindingId = await ResolveTemplateStateBindingIdAsync(requestedBindingId.Value, ct);
            if (!stateBindingId.HasValue)
            {
                throw new InvalidOperationException("Template binding not found.");
            }
        }

        var node = new FunctionNode
        {
            ParentId = request.ParentId,
            Code = request.Code.Trim(),
            Name = resolvedName,
            DisplayName = displayName,
            Route = string.IsNullOrWhiteSpace(request.Route) ? null : request.Route.Trim(),
            Icon = request.Icon?.Trim(),
            IsMenu = request.IsMenu,
            SortOrder = request.SortOrder,
            TemplateStateBindingId = stateBindingId
        };

        _db.FunctionNodes.Add(node);
        await _db.SaveChangesAsync(ct);
        return node;
    }

    public async Task<FunctionNode> UpdateFunctionAsync(Guid id, UpdateFunctionRequest request, CancellationToken ct = default)
    {
        var node = await _db.FunctionNodes.FindAsync(new object[] { id }, ct);
        if (node == null)
        {
            throw new InvalidOperationException("Function node not found.");
        }

        if (request.ParentId.HasValue)
        {
            if (request.ParentId.Value == id)
            {
                throw new InvalidOperationException("Node cannot be its own parent.");
            }

            var parentExists = await _db.FunctionNodes.AnyAsync(f => f.Id == request.ParentId.Value, ct);
            if (!parentExists)
            {
                throw new InvalidOperationException("Parent node does not exist.");
            }

            node.ParentId = request.ParentId;
        }
        else if (request.ClearParent)
        {
            node.ParentId = null;
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            node.Name = request.Name.Trim();
        }

        if (request.DisplayName != null)
        {
            node.DisplayName = new Dictionary<string, string?>(request.DisplayName, StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                node.Name = MultilingualTextHelper.Resolve(node.DisplayName, node.Name);
            }
        }

        if (request.ClearRoute)
        {
            node.Route = null;
        }
        else if (request.Route != null)
        {
            node.Route = string.IsNullOrWhiteSpace(request.Route) ? null : request.Route.Trim();
        }

        if (request.Icon != null)
        {
            node.Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim();
        }

        if (request.IsMenu.HasValue)
        {
            node.IsMenu = request.IsMenu.Value;
        }

        if (request.SortOrder.HasValue)
        {
            node.SortOrder = request.SortOrder.Value;
        }

        if (request.ClearTemplate)
        {
            node.TemplateStateBindingId = null;
            node.TemplateStateBinding = null;
        }
        else
        {
            var requestedBindingId = request.TemplateStateBindingId ?? request.TemplateId;
            if (requestedBindingId.HasValue)
            {
                var stateBindingId = await ResolveTemplateStateBindingIdAsync(requestedBindingId.Value, ct);
                if (!stateBindingId.HasValue)
                {
                    throw new InvalidOperationException("Template binding not found.");
                }

                node.TemplateStateBindingId = stateBindingId.Value;
            }
        }

        if (node.TemplateStateBindingId.HasValue && node.TemplateStateBinding == null)
        {
            await _db.Entry(node).Reference(n => n.TemplateStateBinding).LoadAsync(ct);
        }
        else if (!node.TemplateStateBindingId.HasValue)
        {
            node.TemplateStateBinding = null;
        }

        await _db.SaveChangesAsync(ct);
        return node;
    }

    private async Task<int?> ResolveTemplateStateBindingIdAsync(int requestedId, CancellationToken ct)
    {
        var stateBinding = await _db.TemplateStateBindings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == requestedId, ct);
        if (stateBinding != null)
        {
            return stateBinding.Id;
        }

        var legacyBinding = await _db.TemplateBindings
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == requestedId, ct);
        if (legacyBinding != null)
        {
            var viewState = MapUsageToViewState(legacyBinding.UsageType);
            var mapped = await _db.TemplateStateBindings
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    b => b.EntityType == legacyBinding.EntityType &&
                         b.ViewState == viewState &&
                         b.TemplateId == legacyBinding.TemplateId,
                    ct)
                ?? await _db.TemplateStateBindings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.TemplateId == legacyBinding.TemplateId, ct);

            return mapped?.Id;
        }

        var template = await _db.FormTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == requestedId, ct);
        if (template != null)
        {
            var mapped = await _db.TemplateStateBindings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.TemplateId == template.Id, ct);
            return mapped?.Id;
        }

        return null;
    }

    private static string MapUsageToViewState(FormTemplateUsageType usageType) => usageType switch
    {
        FormTemplateUsageType.List => "List",
        FormTemplateUsageType.Edit => "DetailEdit",
        FormTemplateUsageType.Combined => "Create",
        _ => "DetailView"
    };

    public async Task<List<FunctionNodeDto>> GetMyFunctionsAsync(string userId, string? lang = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        var now = DateTime.UtcNow;
        var functionIds = await _db.RoleAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId &&
                        (!a.ValidFrom.HasValue || a.ValidFrom <= now) &&
                        (!a.ValidTo.HasValue || a.ValidTo >= now))
            .Join(_db.RoleFunctionPermissions.AsNoTracking(),
                a => a.RoleId,
                rfp => rfp.RoleId,
                (assignment, permission) => permission.FunctionId)
            .Distinct()
            .ToListAsync(ct);

        var nodes = await _db.FunctionNodes
            .AsNoTracking()
            .Include(f => f.Template)
            .OrderBy(f => f.SortOrder)
            .ToListAsync(ct);

        if (nodes.Count == 0)
        {
            return new List<FunctionNodeDto>();
        }

        var dict = nodes.ToDictionary(n => n.Id);
        var allowed = new HashSet<Guid>(functionIds);
        if (nodes.FirstOrDefault(n => n.Code == "APP.ROOT") is { } rootNode)
        {
            allowed.Add(rootNode.Id);
        }

        foreach (var id in functionIds)
        {
            var current = id;
            while (dict.TryGetValue(current, out var node) && node.ParentId.HasValue)
            {
                var parentId = node.ParentId.Value;
                if (!allowed.Add(parentId))
                {
                    break;
                }

                current = parentId;
            }
        }

        var filtered = nodes.Where(n => allowed.Contains(n.Id)).ToList();
        if (filtered.Count == 0)
        {
            return new List<FunctionNodeDto>();
        }

        var treeBuilder = new FunctionTreeBuilder(_db, _multilingual);
        return await treeBuilder.BuildAsync(filtered, lang, ct);
    }

    public async Task DeleteFunctionAsync(Guid id, CancellationToken ct = default)
    {
        var node = await _db.FunctionNodes
            .Include(n => n.Children)
            .FirstOrDefaultAsync(n => n.Id == id, ct);

        if (node == null)
        {
            throw new InvalidOperationException("Function node not found.");
        }

        if (node.Children.Count > 0)
        {
            throw new InvalidOperationException("Cannot delete node with children.");
        }

        var referenced = await _db.RoleFunctionPermissions.AnyAsync(rf => rf.FunctionId == id, ct);
        if (referenced)
        {
            throw new InvalidOperationException("Cannot delete node referenced by roles.");
        }

        _db.FunctionNodes.Remove(node);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ReorderFunctionsAsync(IEnumerable<FunctionOrderUpdate> updates, CancellationToken ct = default)
    {
        var updateList = updates.ToList();
        if (updateList.Count == 0)
        {
            return;
        }

        var ids = updateList.Select(u => u.Id).ToList();
        var nodes = await _db.FunctionNodes.Where(n => ids.Contains(n.Id)).ToListAsync(ct);

        var parentMap = await _db.FunctionNodes
            .AsNoTracking()
            .Select(n => new { n.Id, n.ParentId })
            .ToDictionaryAsync(n => n.Id, n => n.ParentId, ct);

        foreach (var update in updateList)
        {
            var node = nodes.FirstOrDefault(n => n.Id == update.Id);
            if (node == null)
            {
                throw new InvalidOperationException($"Function node {update.Id} not found.");
            }

            if (update.ParentId.HasValue && !parentMap.ContainsKey(update.ParentId.Value))
            {
                throw new InvalidOperationException("Cannot move node under a non-existent parent.");
            }

            if (update.ParentId.HasValue && update.ParentId.Value == update.Id)
            {
                throw new InvalidOperationException("Cannot set a node as its own parent.");
            }

            if (!parentMap.ContainsKey(update.Id))
            {
                parentMap[update.Id] = node.ParentId;
            }

            var currentParentId = update.ParentId;
            var visited = new HashSet<Guid>();

            while (currentParentId.HasValue)
            {
                var currentValue = currentParentId.Value;

                if (!visited.Add(currentValue))
                {
                    throw new InvalidOperationException("Detected a cycle in the menu hierarchy.");
                }

                if (currentValue == update.Id)
                {
                    throw new InvalidOperationException("Cannot move a node under its own descendant.");
                }

                if (!parentMap.TryGetValue(currentValue, out var nextParent) || !nextParent.HasValue)
                {
                    break;
                }

                currentParentId = nextParent;
            }

            parentMap[update.Id] = update.ParentId;

            node.ParentId = update.ParentId;
            node.SortOrder = update.SortOrder;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyDictionary<string, FunctionNode>> EnsureEntityMenuAsync(
        EntityDefinition entity,
        IReadOnlyDictionary<string, TemplateStateBinding> bindings,
        CancellationToken ct = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        if (bindings == null)
        {
            throw new ArgumentNullException(nameof(bindings));
        }

        var bindingEntityType = !string.IsNullOrWhiteSpace(entity.EntityRoute)
            ? entity.EntityRoute
            : entity.EntityName.Trim().ToLowerInvariant();

        var legacyBindings = await _db.TemplateBindings
            .AsNoTracking()
            .Where(b => b.EntityType == bindingEntityType && b.IsSystem)
            .ToListAsync(ct);

        var legacyByUsage = legacyBindings
            .GroupBy(b => b.UsageType)
            .ToDictionary(g => g.Key, g => g.First());

        var result = new Dictionary<string, FunctionNode>();

        var root = await EnsureFunctionNodeAsync(
            code: "APP.ROOT",
            name: "应用根节点",
            parentId: null,
            route: null,
            icon: "appstore",
            isMenu: true,
            sortOrder: 0,
            ct);

        var parent = await EnsureFunctionNodeAsync(
            code: "CRM.CORE",
            name: "基本档案",
            parentId: root.Id,
            route: null,
            icon: "database",
            isMenu: true,
            sortOrder: 31,
            ct);

        var codes = BuildFunctionCodes(entity.EntityRoute);
        var displayName = ResolveDisplayName(entity);
        var listRoute = ResolveListRoute(entity);
        var listNode = await EnsureFunctionNodeAsync(
            codes.ListCode,
            displayName,
            parent.Id,
            listRoute,
            entity.Icon ?? "profile",
            isMenu: true,
            sortOrder: 500 + entity.Order,
            ct);
        if (bindings.TryGetValue("List", out var listBinding))
        {
            AttachStateBinding(listNode, listBinding);
        }
        result["List"] = listNode;

        if (bindings.TryGetValue("DetailView", out _))
        {
            var detailNode = await EnsureFunctionNodeAsync(
                codes.DetailCode,
                $"{displayName} Detail",
                listNode.Id,
                null,
                entity.Icon ?? "profile",
                isMenu: false,
                sortOrder: listNode.SortOrder + 1,
                ct);
            if (bindings.TryGetValue("DetailView", out var detailBinding))
            {
                AttachStateBinding(detailNode, detailBinding);
            }
            result["DetailView"] = detailNode;
        }

        if (bindings.TryGetValue("DetailEdit", out _))
        {
            var editNode = await EnsureFunctionNodeAsync(
                codes.EditCode,
                $"{displayName} Edit",
                listNode.Id,
                null,
                entity.Icon ?? "profile",
                isMenu: false,
                sortOrder: listNode.SortOrder + 2,
                ct);
            if (bindings.TryGetValue("DetailEdit", out var editBinding))
            {
                AttachStateBinding(editNode, editBinding);
            }
            result["DetailEdit"] = editNode;
        }

        if (bindings.TryGetValue("Create", out _))
        {
            var createNode = await EnsureFunctionNodeAsync(
                $"{codes.EditCode}.CREATE",
                $"{displayName} Create",
                listNode.Id,
                null,
                entity.Icon ?? "profile",
                isMenu: false,
                sortOrder: listNode.SortOrder + 3,
                ct);
            if (bindings.TryGetValue("Create", out var createBinding))
            {
                AttachStateBinding(createNode, createBinding);
            }
            result["Create"] = createNode;
        }

        await _db.SaveChangesAsync(ct);

        var adminRole = await _db.RoleProfiles
            .Include(r => r.Functions)
            .FirstOrDefaultAsync(r => r.IsSystem, ct);

        if (adminRole != null)
        {
            foreach (var (viewState, node) in result)
            {
                var permission = adminRole.Functions.FirstOrDefault(f => f.FunctionId == node.Id);

                if (permission == null)
                {
                    permission = new RoleFunctionPermission
                    {
                        RoleId = adminRole.Id,
                        FunctionId = node.Id
                    };
                    adminRole.Functions.Add(permission);
                }

                var usage = MapViewStateToUsage(viewState);
                if (legacyByUsage.TryGetValue(usage, out var legacyBinding))
                {
                    permission.TemplateBindingId = legacyBinding.Id;
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        return result;
    }

    public async Task<FunctionNode> EnsureFunctionNodeAsync(
        string code,
        string name,
        Guid? parentId,
        string? route,
        string? icon,
        bool isMenu,
        int sortOrder,
        CancellationToken ct)
    {
        var node = await _db.FunctionNodes.FirstOrDefaultAsync(f => f.Code == code, ct);
        if (node == null)
        {
            node = new FunctionNode { Code = code };
            _db.FunctionNodes.Add(node);
        }

        node.Name = name;
        node.ParentId = parentId;
        node.Route = route;
        node.Icon = icon;
        node.IsMenu = isMenu;
        node.SortOrder = sortOrder;

        return node;
    }

    public async Task<(int imported, int skipped)> ImportFunctionsAsync(MenuImportRequest request, CancellationToken ct = default)
    {
        // Check for conflicts
        var existingCodesList = await _db.FunctionNodes
            .AsNoTracking()
            .Select(f => f.Code)
            .ToListAsync(ct);
        var existingCodes = new HashSet<string>(existingCodesList);

        var importCodes = ExtractAllCodes(request.Functions);
        var conflicts = importCodes.Where(code => existingCodes.Contains(code)).ToList();

        if (conflicts.Count > 0 && request.MergeStrategy != "replace")
        {
             throw new ConflictException("Conflicts detected.", conflicts);
        }

        // Import function nodes
        var importedCount = 0;
        var skippedCount = 0;

        foreach (var funcNode in request.Functions)
        {
            var result = await ImportFunctionNode(funcNode, null, existingCodes, request.MergeStrategy, ct);
            importedCount += result.imported;
            skippedCount += result.skipped;
        }

        await _db.SaveChangesAsync(ct);
        return (importedCount, skippedCount);
    }
    
    // Helper Exception for Conflict
    public class ConflictException : Exception
    {
        public List<string> Conflicts { get; }
        public ConflictException(string message, List<string> conflicts) : base(message)
        {
            Conflicts = conflicts;
        }
    }

    private static void AttachStateBinding(FunctionNode node, TemplateStateBinding binding)
    {
        node.TemplateStateBindingId = binding.Id;
        node.TemplateStateBinding = binding;
    }

    private static (string ListCode, string DetailCode, string EditCode) BuildFunctionCodes(string entityRoute)
    {
        var baseCode = $"CRM.CORE.{entityRoute.ToUpperInvariant()}";
        return (baseCode, $"{baseCode}.DETAIL", $"{baseCode}.EDIT");
    }

    private static string ResolveDisplayName(EntityDefinition entity)
    {
        if (entity.DisplayName != null)
        {
            var value = entity.DisplayName.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return entity.EntityName;
    }

    private static string? ResolveListRoute(EntityDefinition entity)
    {
        if (string.IsNullOrWhiteSpace(entity.ApiEndpoint))
        {
            return null;
        }

        if (entity.ApiEndpoint.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            return entity.ApiEndpoint[4..];
        }

        return entity.ApiEndpoint;
    }

    private static List<string> ExtractAllCodes(List<MenuImportNode> nodes)
    {
        var codes = new List<string>();
        foreach (var node in nodes)
        {
            codes.Add(node.Code);
            if (node.Children != null && node.Children.Count > 0)
            {
                codes.AddRange(ExtractAllCodes(node.Children));
            }
        }
        return codes;
    }

    private async Task<(int imported, int skipped)> ImportFunctionNode(
        MenuImportNode importNode,
        Guid? parentId,
        HashSet<string> existingCodes,
        string? mergeStrategy,
        CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        // Check availability
        var existing = await _db.FunctionNodes
            .FirstOrDefaultAsync(f => f.Code == importNode.Code, ct);

        if (existing != null)
        {
            if (mergeStrategy == "replace")
            {
                // Replace
                existing.Name = importNode.Name ?? existing.Name;
                existing.DisplayName = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase); 
                if (importNode.DisplayName?.Count > 0)
                {
                     foreach(var kv in importNode.DisplayName) existing.DisplayName[kv.Key] = kv.Value;
                }
                
                existing.Route = importNode.Route;
                existing.Icon = importNode.Icon;
                existing.IsMenu = importNode.IsMenu;
                existing.SortOrder = importNode.SortOrder;
                imported++;
            }
            else
            {
                // Skip
                skipped++;
                return (imported, skipped);
            }
        }
        else
        {
            // Create New
            var newNode = new FunctionNode
            {
                Id = Guid.NewGuid(),
                ParentId = parentId,
                Code = importNode.Code,
                Name = importNode.Name ?? importNode.Code,
                DisplayName = importNode.DisplayName != null ? new Dictionary<string, string?>(importNode.DisplayName) : new Dictionary<string, string?>(),
                Route = importNode.Route,
                Icon = importNode.Icon,
                IsMenu = importNode.IsMenu,
                SortOrder = importNode.SortOrder
            };
            _db.FunctionNodes.Add(newNode);
            imported++;
            existing = newNode;
        }

        // Recursively import children
        if (importNode.Children != null && importNode.Children.Count > 0)
        {
            foreach (var child in importNode.Children)
            {
                var result = await ImportFunctionNode(child, existing.Id, existingCodes, mergeStrategy, ct);
                imported += result.imported;
                skipped += result.skipped;
            }
        }

        return (imported, skipped);
    }

    private static FormTemplateUsageType MapViewStateToUsage(string viewState)
    {
        if (string.IsNullOrWhiteSpace(viewState))
        {
            return FormTemplateUsageType.Detail;
        }

        return viewState switch
        {
            "List" => FormTemplateUsageType.List,
            "DetailEdit" => FormTemplateUsageType.Edit,
            "Create" => FormTemplateUsageType.Combined,
            _ => FormTemplateUsageType.Detail
        };
    }
}
