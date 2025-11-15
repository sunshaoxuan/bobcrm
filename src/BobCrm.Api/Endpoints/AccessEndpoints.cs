using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BobCrm.Api.Base;
using System.Collections.Generic;
using System.Linq;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Middleware;
using BobCrm.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Endpoints;

public static class AccessEndpoints
{
    public static IEndpointRouteBuilder MapAccessEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/access").RequireAuthorization();

        group.MapGet("/functions", async ([FromServices] AppDbContext db, CancellationToken ct) =>
        {
            var nodes = await db.FunctionNodes
                .AsNoTracking()
                .Include(f => f.Template)
                .OrderBy(f => f.SortOrder)
                .ToListAsync(ct);
            var localizedNames = await LoadFunctionNameTranslationsAsync(db, nodes, ct);
            var bindings = await LoadTemplateBindingsAsync(db, ct);
            return Results.Ok(BuildTree(nodes, localizedNames, bindings));
            var templateOptions = await LoadTemplateOptionsAsync(db, ct);
            return Results.Ok(BuildTree(nodes, templateOptions));
        }).RequireFunction("BAS.AUTH.ROLE.PERM");

        group.MapGet("/functions/manage", async ([FromServices] AppDbContext db, CancellationToken ct) =>
        {
            var nodes = await db.FunctionNodes
                .AsNoTracking()
                .Include(f => f.Template)
                .OrderBy(f => f.SortOrder)
                .ToListAsync(ct);
            return Results.Ok(BuildTree(nodes));
        }).RequireFunction("SYS.SET.MENU");

        group.MapGet("/functions/me", async (
            ClaimsPrincipal user,
            [FromServices] AppDbContext db,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            var now = DateTime.UtcNow;
            var functionIds = await db.RoleAssignments
                .AsNoTracking()
                .Where(a => a.UserId == userId &&
                            (!a.ValidFrom.HasValue || a.ValidFrom <= now) &&
                            (!a.ValidTo.HasValue || a.ValidTo >= now))
                .Join(db.RoleFunctionPermissions.AsNoTracking(),
                    a => a.RoleId,
                    rfp => rfp.RoleId,
                    (assignment, permission) => permission.FunctionId)
                .Distinct()
                .ToListAsync(ct);

            var nodes = await db.FunctionNodes
                .AsNoTracking()
                .Include(f => f.Template)
                .OrderBy(f => f.SortOrder)
                .ToListAsync(ct);

            if (nodes.Count == 0)
            {
                return Results.Ok(new List<FunctionNodeDto>());
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
                allowed.Add(parentId);
                current = parentId;
            }
        }

            var filtered = nodes.Where(n => allowed.Contains(n.Id)).ToList();
            if (filtered.Count == 0)
            {
                return Results.Ok(new List<FunctionNodeDto>());
            }

            var localizedNames = await LoadFunctionNameTranslationsAsync(db, filtered, ct);
            var bindings = await LoadTemplateBindingsAsync(db, ct);
            return Results.Ok(BuildTree(filtered, localizedNames, bindings));
            var templateOptions = await LoadTemplateOptionsAsync(db, ct);
            return Results.Ok(BuildTree(filtered, templateOptions));
        });

        group.MapPost("/functions", async ([FromBody] CreateFunctionRequest request,
            [FromServices] AccessService service,
            [FromServices] AuditTrailService auditTrail,
            CancellationToken ct) =>
        {
            try
            {
                var node = await service.CreateFunctionAsync(request, ct);
                await auditTrail.RecordAsync("MENU", "CREATE", $"Created function {node.Name}", node.Code, new
                {
                    node.Id,
                    node.Code,
                    node.Name,
                    node.DisplayName,
                    node.ParentId,
                    node.Route,
                    node.TemplateId,
                    TemplateName = node.Template?.Name
                }, ct);
                return Results.Ok(ToDto(node));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireFunction("SYS.SET.MENU");

        group.MapPut("/functions/{id:guid}", async (Guid id,
            [FromBody] UpdateFunctionRequest request,
            [FromServices] AccessService service,
            [FromServices] AuditTrailService auditTrail,
            CancellationToken ct) =>
        {
            try
            {
                var node = await service.UpdateFunctionAsync(id, request, ct);
                await auditTrail.RecordAsync("MENU", "UPDATE", $"Updated function {node.Name}", node.Code, new
                {
                    node.Id,
                    node.Code,
                    node.Name,
                    node.DisplayName,
                    node.ParentId,
                    node.SortOrder,
                    node.Route,
                    node.TemplateId,
                    TemplateName = node.Template?.Name
                }, ct);
                return Results.Ok(ToDto(node));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireFunction("SYS.SET.MENU");

        group.MapDelete("/functions/{id:guid}", async (Guid id,
            [FromServices] AccessService service,
            [FromServices] AuditTrailService auditTrail,
            CancellationToken ct) =>
        {
            try
            {
                await service.DeleteFunctionAsync(id, ct);
                await auditTrail.RecordAsync("MENU", "DELETE", $"Deleted function {id}", id.ToString(), null, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireFunction("SYS.SET.MENU");

        group.MapPost("/functions/reorder", async ([FromBody] List<FunctionOrderUpdate> updates,
            [FromServices] AccessService service,
            [FromServices] AuditTrailService auditTrail,
            CancellationToken ct) =>
        {
            try
            {
                await service.ReorderFunctionsAsync(updates, ct);
                await auditTrail.RecordAsync("MENU", "REORDER", "Reordered menu nodes", null, updates, ct);
                return Results.Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireFunction("SYS.SET.MENU");

        group.MapGet("/roles", async ([FromServices] AppDbContext db, CancellationToken ct) =>
        {
            var roles = await db.RoleProfiles
                .AsNoTracking()
                .Include(r => r.Functions)
                .Include(r => r.DataScopes)
                .OrderBy(r => r.Code)
                .ToListAsync(ct);
            return Results.Ok(roles);
        }).RequireFunction("BAS.AUTH.ROLE.PERM");

        group.MapPost("/roles", async ([FromBody] CreateRoleRequest request, [FromServices] AccessService service, CancellationToken ct) =>
        {
            var role = await service.CreateRoleAsync(request, ct);
            return Results.Ok(role);
        }).RequireFunction("BAS.AUTH.ROLE.PERM");

        group.MapGet("/roles/{roleId:guid}", async (Guid roleId, [FromServices] AppDbContext db, CancellationToken ct) =>
        {
            var role = await db.RoleProfiles
                .AsNoTracking()
                .Include(r => r.Functions)
                .ThenInclude(f => f.Function)
                .Include(r => r.DataScopes)
                .FirstOrDefaultAsync(r => r.Id == roleId, ct);

            if (role == null)
                return Results.NotFound(new { error = "Role not found" });

            return Results.Ok(role);
        }).RequireFunction("BAS.AUTH.ROLE.PERM");

        group.MapPut("/roles/{roleId:guid}", async (Guid roleId, [FromBody] UpdateRoleRequest request, [FromServices] AppDbContext db, CancellationToken ct) =>
        {
            var role = await db.RoleProfiles.FindAsync(new object[] { roleId }, ct);
            if (role == null)
                return Results.NotFound(new { error = "Role not found" });

            if (role.IsSystem)
                return Results.BadRequest(new { error = "Cannot update system role" });

            if (!string.IsNullOrWhiteSpace(request.Name))
                role.Name = request.Name;
            if (request.Description is not null)
                role.Description = request.Description;
            if (request.IsEnabled.HasValue)
                role.IsEnabled = request.IsEnabled.Value;

            await db.SaveChangesAsync(ct);
            return Results.Ok(role);
        }).RequireFunction("BAS.AUTH.ROLE.PERM");

        group.MapDelete("/roles/{roleId:guid}", async (Guid roleId, [FromServices] AppDbContext db, CancellationToken ct) =>
        {
            var role = await db.RoleProfiles
                .Include(r => r.Functions)
                .Include(r => r.DataScopes)
                .Include(r => r.Assignments)
                .FirstOrDefaultAsync(r => r.Id == roleId, ct);

            if (role == null)
                return Results.NotFound(new { error = "Role not found" });

            if (role.IsSystem)
                return Results.BadRequest(new { error = "Cannot delete system role" });

            if (role.Assignments?.Any() == true)
                return Results.BadRequest(new { error = "Cannot delete role with assignments" });

            db.RoleProfiles.Remove(role);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireFunction("BAS.AUTH.ROLE.PERM");

        group.MapPut("/roles/{roleId:guid}/permissions", async (Guid roleId, [FromBody] UpdatePermissionsRequest request, [FromServices] AppDbContext db, CancellationToken ct) =>
        {
            var role = await db.RoleProfiles
                .Include(r => r.Functions)
                .Include(r => r.DataScopes)
                .FirstOrDefaultAsync(r => r.Id == roleId, ct);

            if (role == null)
                return Results.NotFound(new { error = "Role not found" });

            // Update function permissions
            db.RoleFunctionPermissions.RemoveRange(role.Functions);
            var templateSelections = request.FunctionPermissions?
                .Where(fp => fp.FunctionId != Guid.Empty)
                .GroupBy(fp => fp.FunctionId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Last().TemplateBindingId);

            var finalFunctionIds = new HashSet<Guid>(request.FunctionIds ?? Enumerable.Empty<Guid>());
            if (templateSelections != null)
            {
                foreach (var functionId in templateSelections.Keys)
                {
                    finalFunctionIds.Add(functionId);
                }
            }

            if (finalFunctionIds.Count > 0)
            {
                role.Functions = finalFunctionIds.Select(fid => new RoleFunctionPermission
                {
                    RoleId = roleId,
                    FunctionId = fid,
                    TemplateBindingId = templateSelections != null && templateSelections.TryGetValue(fid, out var bindingId)
                        ? bindingId
                        : null
                }).ToList();
            }
            else
            {
                role.Functions = new List<RoleFunctionPermission>();
            }

            // Update data scopes
            db.RoleDataScopes.RemoveRange(role.DataScopes);
            if (request.DataScopes?.Count > 0)
            {
                role.DataScopes = request.DataScopes.Select(ds => new RoleDataScope
                {
                    RoleId = roleId,
                    EntityName = ds.EntityName,
                    ScopeType = ds.ScopeType,
                    FilterExpression = ds.FilterExpression
                }).ToList();
            }

            await db.SaveChangesAsync(ct);
            return Results.Ok(new { message = "Permissions updated successfully" });
        }).RequireFunction("BAS.AUTH.ROLE.PERM");

        group.MapPost("/assignments", async ([FromBody] AssignRoleRequest request, [FromServices] AccessService service, CancellationToken ct) =>
        {
            var assignment = await service.AssignRoleAsync(request, ct);
            return Results.Ok(assignment);
        }).RequireFunction("BAS.AUTH.USER.ROLE");

        group.MapGet("/assignments/user/{userId}", async (string userId, [FromServices] AppDbContext db, CancellationToken ct) =>
        {
            var assignments = await db.RoleAssignments
                .AsNoTracking()
                .Include(a => a.Role)
                .Where(a => a.UserId == userId)
                .Select(a => new
                {
                    a.Id,
                    a.RoleId,
                    RoleCode = a.Role!.Code,
                    RoleName = a.Role!.Name,
                    a.OrganizationId,
                    a.ValidFrom,
                    a.ValidTo
                })
                .ToListAsync(ct);

            return Results.Ok(assignments);
        }).RequireFunction("BAS.AUTH.USER.ROLE");

        group.MapDelete("/assignments/{assignmentId:guid}", async (Guid assignmentId, [FromServices] AppDbContext db, CancellationToken ct) =>
        {
            var assignment = await db.RoleAssignments.FindAsync(new object[] { assignmentId }, ct);
            if (assignment == null)
                return Results.NotFound(new { error = "Assignment not found" });

            db.RoleAssignments.Remove(assignment);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireFunction("BAS.AUTH.USER.ROLE");

        return app;
    }

    private static FunctionNodeDto ToDto(FunctionNode node)
    {
        return new FunctionNodeDto
        {
            Id = node.Id,
            ParentId = node.ParentId,
            Code = node.Code,
            Name = node.Name,
            DisplayName = node.DisplayName != null ? new MultilingualText(node.DisplayName) : null,
            Route = node.Route,
            Icon = node.Icon,
            IsMenu = node.IsMenu,
            SortOrder = node.SortOrder,
            TemplateId = node.TemplateId,
            TemplateName = node.Template?.Name,
            Children = new List<FunctionNodeDto>()
        };
    }

    private static List<FunctionNodeDto> BuildTree(List<FunctionNode> nodes)
    {
        var lookup = nodes.ToDictionary(n => n.Id, n => ToDto(n));

        List<FunctionNodeDto> roots = new();
        foreach (var source in nodes.OrderBy(n => n.SortOrder))
        {
            if (!lookup.TryGetValue(source.Id, out var node))
            {
                continue;
            }

            if (node.ParentId.HasValue && lookup.TryGetValue(node.ParentId.Value, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }
        SortChildren(roots);
        return roots;
    }

    private static void SortChildren(List<FunctionNodeDto> nodes)
    {
        nodes.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
        foreach (var node in nodes)
        {
            SortChildren(node.Children);
        }
    }

    private static async Task<Dictionary<Guid, Dictionary<string, string?>>> LoadFunctionNameTranslationsAsync(
        AppDbContext db,
        List<FunctionNode> nodes,
        CancellationToken ct)
    {
        var result = new Dictionary<Guid, Dictionary<string, string?>>(nodes.Count);
        if (nodes.Count == 0)
        {
            return result;
        }

        var languages = await db.LocalizationLanguages
            .AsNoTracking()
            .Select(l => l.Code.ToLower())
            .ToListAsync(ct);

        if (languages.Count == 0)
        {
            languages = new List<string> { "ja", "en", "zh" };
        }

        var codes = nodes.Select(n => n.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var candidateKeys = nodes
            .Select(n => $"MENU_{n.Code.Replace('.', '_')}" )
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var resources = await db.LocalizationResources
            .AsNoTracking()
            .Where(r => codes.Contains(r.Key) || candidateKeys.Contains(r.Key))
            .ToListAsync(ct);

        var resourceLookup = resources.ToDictionary(r => r.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var node in nodes)
        {
            var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            LocalizationResource? resolved = null;
            var keysToTry = new[]
            {
                node.Code,
                $"MENU_{node.Code.Replace('.', '_')}"
            };

            foreach (var key in keysToTry)
            {
                if (resourceLookup.TryGetValue(key, out var resource))
                {
                    resolved = resource;
                    break;
                }
            }

            if (resolved != null)
            {
                foreach (var lang in languages)
                {
                    resolved.Translations.TryGetValue(lang, out var value);
                    map[lang] = string.IsNullOrWhiteSpace(value) ? node.Name : value;
                }
            }
            else
            {
                foreach (var lang in languages)
                {
                    map[lang] = node.Name;
                }
            }

            result[node.Id] = map;
        }

        return result;
    }

    private static async Task<Dictionary<string, List<FunctionNodeTemplateBindingDto>>> LoadTemplateBindingsAsync(
        AppDbContext db,
        CancellationToken ct)
    {
        var bindings = await db.TemplateBindings
            .AsNoTracking()
            .Include(tb => tb.Template)
            .ToListAsync(ct);

        return bindings
            .Select(tb => new
            {
                Code = tb.RequiredFunctionCode ?? tb.Template?.RequiredFunctionCode,
                Binding = new FunctionNodeTemplateBindingDto(
                    tb.Id,
                    tb.EntityType,
                    tb.UsageType,
                    tb.TemplateId,
                    tb.Template?.Name ?? string.Empty,
                    tb.IsSystem)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Code))
            .GroupBy(x => x.Code!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Binding).OrderBy(b => b.UsageType).ThenBy(b => b.TemplateId).ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static string ComputeFunctionTreeVersion(
        IEnumerable<object> functionSnapshot,
        IEnumerable<object> templateSnapshot)
    {
        var builder = new StringBuilder();

        foreach (var entry in functionSnapshot)
        {
            builder.AppendLine(entry.ToString());
        }

        foreach (var entry in templateSnapshot)
        {
            builder.AppendLine(entry.ToString());
        }

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(hash);
    }
}
