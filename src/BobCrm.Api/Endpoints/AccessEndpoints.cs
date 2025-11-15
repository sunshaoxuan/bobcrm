using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using BobCrm.Api.Middleware;
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
                .OrderBy(f => f.SortOrder)
                .ToListAsync(ct);
            var templateOptions = await LoadTemplateOptionsAsync(db, ct);
            return Results.Ok(BuildTree(nodes, templateOptions));
        }).RequireFunction("BAS.AUTH.ROLE.PERM");

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

            var templateOptions = await LoadTemplateOptionsAsync(db, ct);
            return Results.Ok(BuildTree(filtered, templateOptions));
        });

        group.MapPost("/functions", async ([FromBody] CreateFunctionRequest request, [FromServices] AccessService service, CancellationToken ct) =>
        {
            var node = await service.CreateFunctionAsync(request, ct);
            return Results.Ok(node);
        }).RequireFunction("BAS.AUTH.ROLE.PERM");

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

    private static Dictionary<string, List<FunctionTemplateOptionDto>> GroupTemplateOptions(IEnumerable<TemplateBinding> bindings)
    {
        return bindings
            .Select(binding => new
            {
                Binding = binding,
                Code = binding.RequiredFunctionCode ?? binding.Template?.RequiredFunctionCode
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Code))
            .GroupBy(x => x.Code!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g
                    .Select(x => new FunctionTemplateOptionDto
                    {
                        BindingId = x.Binding.Id,
                        TemplateId = x.Binding.TemplateId,
                        TemplateName = x.Binding.Template?.Name ?? $"Template #{x.Binding.TemplateId}",
                        EntityType = x.Binding.EntityType,
                        UsageType = x.Binding.UsageType,
                        IsSystem = x.Binding.IsSystem,
                        IsDefault = x.Binding.Template?.IsSystemDefault ?? false
                    })
                    .OrderBy(o => o.TemplateName, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<Dictionary<string, List<FunctionTemplateOptionDto>>> LoadTemplateOptionsAsync(AppDbContext db, CancellationToken ct)
    {
        var bindings = await db.TemplateBindings
            .AsNoTracking()
            .Include(tb => tb.Template)
            .Where(tb => tb.RequiredFunctionCode != null || (tb.Template != null && tb.Template.RequiredFunctionCode != null))
            .ToListAsync(ct);

        return GroupTemplateOptions(bindings);
    }

    private static List<FunctionNodeDto> BuildTree(List<FunctionNode> nodes, Dictionary<string, List<FunctionTemplateOptionDto>> templateOptions)
    {
        var lookup = nodes.ToDictionary(n => n.Id, n => new FunctionNodeDto
        {
            Id = n.Id,
            ParentId = n.ParentId,
            Code = n.Code,
            Name = n.Name,
            Route = n.Route,
            Icon = n.Icon,
            IsMenu = n.IsMenu,
            SortOrder = n.SortOrder,
            TemplateOptions = n.Code != null && templateOptions.TryGetValue(n.Code, out var options)
                ? new List<FunctionTemplateOptionDto>(options)
                : new List<FunctionTemplateOptionDto>()
        });

        List<FunctionNodeDto> roots = new();
        foreach (var node in lookup.Values)
        {
            if (node.ParentId.HasValue && lookup.TryGetValue(node.ParentId.Value, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }
        return roots.OrderBy(r => r.SortOrder).ToList();
    }
}
