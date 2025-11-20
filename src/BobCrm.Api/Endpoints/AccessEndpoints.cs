using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using BobCrm.Api.Base;
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

        group.MapGet("/functions", async (
            [FromServices] AppDbContext db,
            [FromServices] FunctionTreeBuilder treeBuilder,
            CancellationToken ct) =>
        {
            var nodes = await db.FunctionNodes
                .AsNoTracking()
                .Include(f => f.Template)
                .OrderBy(f => f.SortOrder)
                .ToListAsync(ct);
            var tree = await treeBuilder.BuildAsync(nodes, ct);
            return Results.Ok(tree);
        }).RequireFunction("BAS.AUTH.ROLE.PERM");

        group.MapGet("/functions/manage", async (
            [FromServices] AppDbContext db,
            [FromServices] FunctionTreeBuilder treeBuilder,
            CancellationToken ct) =>
        {
            var nodes = await db.FunctionNodes
                .AsNoTracking()
                .Include(f => f.Template)
                .OrderBy(f => f.SortOrder)
                .ToListAsync(ct);
            var tree = await treeBuilder.BuildAsync(nodes, ct);
            return Results.Ok(tree);
        }).RequireFunction("SYS.SET.MENU");

        group.MapGet("/functions/me", async (
            ClaimsPrincipal user,
            [FromServices] AppDbContext db,
            [FromServices] FunctionTreeBuilder treeBuilder,
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

            var tree = await treeBuilder.BuildAsync(filtered, ct);
            return Results.Ok(tree);
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

        group.MapGet("/functions/export", async (
            [FromServices] AppDbContext db,
            CancellationToken ct) =>
        {
            var nodes = await db.FunctionNodes
                .AsNoTracking()
                .OrderBy(f => f.SortOrder)
                .ToListAsync(ct);

            var rootNodes = nodes.Where(n => !n.ParentId.HasValue).ToList();
            var lookup = nodes.ToDictionary(n => n.Id);

            var exportData = new
            {
                version = "1.0",
                exportDate = DateTime.UtcNow,
                functions = rootNodes.Select(node => BuildExportNode(node, lookup)).ToList()
            };

            return Results.Ok(exportData);
        }).RequireFunction("SYS.SET.MENU");

        group.MapPost("/functions/import", async (
            [FromBody] MenuImportRequest request,
            [FromServices] AppDbContext db,
            [FromServices] AuditTrailService auditTrail,
            CancellationToken ct) =>
        {
            try
            {
                // 检查冲突
                var existingCodes = await db.FunctionNodes
                    .AsNoTracking()
                    .Select(f => f.Code)
                    .ToHashSetAsync(ct);

                var importCodes = ExtractAllCodes(request.Functions);
                var conflicts = importCodes.Where(code => existingCodes.Contains(code)).ToList();

                if (conflicts.Count > 0 && request.MergeStrategy != "replace")
                {
                    return Results.BadRequest(new
                    {
                        error = "功能码冲突",
                        conflicts = conflicts,
                        message = "存在冲突的功能码。请选择合并策略：'replace'（替换现有）或 'skip'（跳过冲突）"
                    });
                }

                // 导入功能节点
                var importedCount = 0;
                var skippedCount = 0;

                foreach (var funcNode in request.Functions)
                {
                    var result = await ImportFunctionNode(funcNode, null, db, existingCodes, request.MergeStrategy, ct);
                    importedCount += result.imported;
                    skippedCount += result.skipped;
                }

                await db.SaveChangesAsync(ct);
                await auditTrail.RecordAsync("MENU", "IMPORT", $"Imported {importedCount} functions, skipped {skippedCount}", null, new
                {
                    importedCount,
                    skippedCount,
                    strategy = request.MergeStrategy
                }, ct);

                return Results.Ok(new
                {
                    message = "导入成功",
                    imported = importedCount,
                    skipped = skippedCount
                });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = $"导入失败: {ex.Message}" });
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
            return Results.Ok(roles.Select(ToRoleDto).ToList());
        }).RequireFunction("BAS.AUTH.ROLE.PERM");

        group.MapPost("/roles", async ([FromBody] CreateRoleRequest request, [FromServices] AccessService service, CancellationToken ct) =>
        {
            var role = await service.CreateRoleAsync(request, ct);
            return Results.Ok(ToRoleDto(role));
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

            return Results.Ok(ToRoleDto(role));
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
            await db.Entry(role).Collection(r => r.Functions).LoadAsync(ct);
            await db.Entry(role).Collection(r => r.DataScopes).LoadAsync(ct);
            return Results.Ok(ToRoleDto(role));
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

    private static RoleProfileDto ToRoleDto(RoleProfile role)
    {
        return new RoleProfileDto
        {
            Id = role.Id,
            OrganizationId = role.OrganizationId,
            Code = role.Code,
            Name = role.Name,
            Description = role.Description,
            IsSystem = role.IsSystem,
            IsEnabled = role.IsEnabled,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt,
            Functions = (role.Functions ?? new List<RoleFunctionPermission>())
                .Select(f => new RoleFunctionDto
                {
                    RoleId = f.RoleId,
                    FunctionId = f.FunctionId,
                    TemplateBindingId = f.TemplateBindingId
                })
                .ToList(),
            DataScopes = (role.DataScopes ?? new List<RoleDataScope>())
                .Select(ds => new RoleDataScopeDto
                {
                    Id = ds.Id,
                    EntityName = ds.EntityName,
                    ScopeType = ds.ScopeType,
                    FilterExpression = ds.FilterExpression
                })
                .ToList()
        };
    }

    private static FunctionNodeDto ToDto(FunctionNode node)
    {
        return new FunctionNodeDto
        {
            Id = node.Id,
            ParentId = node.ParentId,
            Code = node.Code,
            Name = node.Name,
            DisplayNameTranslations = node.DisplayName != null ? new BobCrm.Api.Contracts.DTOs.MultilingualText(node.DisplayName) : null,
            Route = node.Route,
            Icon = node.Icon,
            IsMenu = node.IsMenu,
            SortOrder = node.SortOrder,
            TemplateId = node.TemplateId,
            TemplateName = node.Template?.Name,
            Children = new List<FunctionNodeDto>(),
            TemplateOptions = new List<FunctionTemplateOptionDto>(),
            TemplateBindings = new List<FunctionNodeTemplateBindingDto>()
        };
    }

    private static object BuildExportNode(FunctionNode node, Dictionary<Guid, FunctionNode> lookup)
    {
        var children = lookup.Values
            .Where(n => n.ParentId == node.Id)
            .OrderBy(n => n.SortOrder)
            .Select(child => BuildExportNode(child, lookup))
            .ToList();

        return new
        {
            code = node.Code,
            name = node.Name,
            displayName = node.DisplayName,
            route = node.Route,
            icon = node.Icon,
            isMenu = node.IsMenu,
            sortOrder = node.SortOrder,
            children = children.Count > 0 ? children : null
        };
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

    private static async Task<(int imported, int skipped)> ImportFunctionNode(
        MenuImportNode importNode,
        Guid? parentId,
        AppDbContext db,
        HashSet<string> existingCodes,
        string? mergeStrategy,
        CancellationToken ct)
    {
        var imported = 0;
        var skipped = 0;

        // 检查是否已存在
        var existing = await db.FunctionNodes
            .FirstOrDefaultAsync(f => f.Code == importNode.Code, ct);

        if (existing != null)
        {
            if (mergeStrategy == "replace")
            {
                // 替换现有节点
                existing.Name = importNode.Name ?? existing.Name;
                existing.DisplayName = importNode.DisplayName;
                existing.Route = importNode.Route;
                existing.Icon = importNode.Icon;
                existing.IsMenu = importNode.IsMenu;
                existing.SortOrder = importNode.SortOrder;
                imported++;
            }
            else
            {
                // 跳过
                skipped++;
                return (imported, skipped);
            }
        }
        else
        {
            // 创建新节点
            var newNode = new FunctionNode
            {
                Id = Guid.NewGuid(),
                ParentId = parentId,
                Code = importNode.Code,
                Name = importNode.Name ?? importNode.Code,
                DisplayName = importNode.DisplayName,
                Route = importNode.Route,
                Icon = importNode.Icon,
                IsMenu = importNode.IsMenu,
                SortOrder = importNode.SortOrder
            };
            db.FunctionNodes.Add(newNode);
            imported++;
            existing = newNode;
        }

        // 递归导入子节点
        if (importNode.Children != null && importNode.Children.Count > 0)
        {
            foreach (var child in importNode.Children)
            {
                var result = await ImportFunctionNode(child, existing.Id, db, existingCodes, mergeStrategy, ct);
                imported += result.imported;
                skipped += result.skipped;
            }
        }

        return (imported, skipped);
    }

}
