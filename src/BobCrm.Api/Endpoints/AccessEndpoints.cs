using System.Security.Claims;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Domain.Models;
using BobCrm.Api.Infrastructure;
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
                .OrderBy(f => f.SortOrder)
                .ToListAsync(ct);
            return Results.Ok(BuildTree(nodes));
        });

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

            return Results.Ok(BuildTree(filtered));
        });

        group.MapPost("/functions", async ([FromBody] CreateFunctionRequest request, [FromServices] AccessService service, CancellationToken ct) =>
        {
            var node = await service.CreateFunctionAsync(request, ct);
            return Results.Ok(node);
        });

        group.MapGet("/roles", async ([FromServices] AppDbContext db, CancellationToken ct) =>
        {
            var roles = await db.RoleProfiles
                .AsNoTracking()
                .Include(r => r.Functions)
                .Include(r => r.DataScopes)
                .OrderBy(r => r.Code)
                .ToListAsync(ct);
            return Results.Ok(roles);
        });

        group.MapPost("/roles", async ([FromBody] CreateRoleRequest request, [FromServices] AccessService service, CancellationToken ct) =>
        {
            var role = await service.CreateRoleAsync(request, ct);
            return Results.Ok(role);
        });

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
        });

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
        });

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
        });

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
            if (request.FunctionIds?.Count > 0)
            {
                role.Functions = request.FunctionIds.Select(fid => new RoleFunctionPermission
                {
                    RoleId = roleId,
                    FunctionId = fid
                }).ToList();
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
        });

        group.MapPost("/assignments", async ([FromBody] AssignRoleRequest request, [FromServices] AccessService service, CancellationToken ct) =>
        {
            var assignment = await service.AssignRoleAsync(request, ct);
            return Results.Ok(assignment);
        });

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
        });

        group.MapDelete("/assignments/{assignmentId:guid}", async (Guid assignmentId, [FromServices] AppDbContext db, CancellationToken ct) =>
        {
            var assignment = await db.RoleAssignments.FindAsync(new object[] { assignmentId }, ct);
            if (assignment == null)
                return Results.NotFound(new { error = "Assignment not found" });

            db.RoleAssignments.Remove(assignment);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        return app;
    }

    private static List<FunctionNodeDto> BuildTree(List<FunctionNode> nodes)
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
            SortOrder = n.SortOrder
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
