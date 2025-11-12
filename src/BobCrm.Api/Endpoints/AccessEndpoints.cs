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

        group.MapPost("/assignments", async ([FromBody] AssignRoleRequest request, [FromServices] AccessService service, CancellationToken ct) =>
        {
            var assignment = await service.AssignRoleAsync(request, ct);
            return Results.Ok(assignment);
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
