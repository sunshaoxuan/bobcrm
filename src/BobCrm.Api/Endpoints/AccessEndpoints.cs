using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.DTOs.Access;
using BobCrm.Api.Contracts.Requests.Access;
using BobCrm.Api.Contracts.Responses.Access;
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
            string? lang,
            HttpContext http,
            [FromServices] AppDbContext db,
            [FromServices] FunctionTreeBuilder treeBuilder,
            CancellationToken ct) =>
        {
            var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
            var nodes = await db.FunctionNodes
                .AsNoTracking()
                .Include(f => f.Template)
                .OrderBy(f => f.SortOrder)
                .ToListAsync(ct);
            var tree = await treeBuilder.BuildAsync(nodes, lang: targetLang, ct: ct);
            return Results.Ok(new SuccessResponse<List<FunctionNodeDto>>(tree));
        }).RequireFunction("BAS.AUTH.ROLE.PERM");

        group.MapGet("/functions/manage", async (
            string? lang,
            HttpContext http,
            [FromServices] AppDbContext db,
            [FromServices] FunctionTreeBuilder treeBuilder,
            CancellationToken ct) =>
        {
            var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
            var nodes = await db.FunctionNodes
                .AsNoTracking()
                .Include(f => f.Template)
                .OrderBy(f => f.SortOrder)
                .ToListAsync(ct);
            var tree = await treeBuilder.BuildAsync(nodes, lang: targetLang, ct: ct);
            return Results.Ok(new SuccessResponse<List<FunctionNodeDto>>(tree));
        }).RequireFunction("SYS.SET.MENU");

        group.MapGet("/functions/me", async (
            string? lang,
            HttpContext http,
            ClaimsPrincipal user,
            [FromServices] AccessService accessService,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            var targetLang = LangHelper.GetLang(http, lang);
            var tree = await accessService.GetMyFunctionsAsync(userId, targetLang, ct);
            return Results.Ok(new SuccessResponse<List<FunctionNodeDto>>(tree));
        });

        group.MapPost("/functions", async ([FromBody] CreateFunctionRequest request,
            [FromQuery] string? lang,
            [FromServices] AccessService service,
            [FromServices] FunctionTreeBuilder treeBuilder,
            [FromServices] AuditTrailService auditTrail,
            [FromServices] ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            var uiLang = LangHelper.GetLang(http);
            var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
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
                return Results.Ok(new SuccessResponse<FunctionNodeDto>(await ToDtoAsync(node, treeBuilder, targetLang, ct)));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse(string.Format(loc.T("ERR_FUNCTION_CREATE_FAILED", uiLang), ex.Message), "FUNCTION_CREATE_FAILED"));
            }
        }).RequireFunction("SYS.SET.MENU");

        group.MapPut("/functions/{id:guid}", async (Guid id,
            [FromBody] UpdateFunctionRequest request,
            [FromQuery] string? lang,
            [FromServices] AccessService service,
            [FromServices] FunctionTreeBuilder treeBuilder,
            [FromServices] AuditTrailService auditTrail,
            [FromServices] ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            var uiLang = LangHelper.GetLang(http);
            var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
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
                return Results.Ok(new SuccessResponse<FunctionNodeDto>(await ToDtoAsync(node, treeBuilder, targetLang, ct)));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse(string.Format(loc.T("ERR_FUNCTION_UPDATE_FAILED", uiLang), ex.Message), "FUNCTION_UPDATE_FAILED"));
            }
        }).RequireFunction("SYS.SET.MENU");

        group.MapDelete("/functions/{id:guid}", async (Guid id,
            [FromServices] AccessService service,
            [FromServices] AuditTrailService auditTrail,
            [FromServices] ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
            try
            {
                await service.DeleteFunctionAsync(id, ct);
                await auditTrail.RecordAsync("MENU", "DELETE", $"Deleted function {id}", id.ToString(), null, ct);
                return Results.Ok(ApiResponseExtensions.SuccessResponse(loc.T("MSG_FUNCTION_DELETED", lang)));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse(string.Format(loc.T("ERR_FUNCTION_DELETE_FAILED", lang), ex.Message), "FUNCTION_DELETE_FAILED"));
            }
        }).RequireFunction("SYS.SET.MENU");

        group.MapPost("/functions/reorder", async ([FromBody] List<FunctionOrderUpdate> updates,
            [FromServices] AccessService service,
            [FromServices] AuditTrailService auditTrail,
            [FromServices] ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
            try
            {
                await service.ReorderFunctionsAsync(updates, ct);
                await auditTrail.RecordAsync("MENU", "REORDER", "Reordered menu nodes", null, updates, ct);
                return Results.Ok(new SuccessResponse(loc.T("MSG_FUNCTIONS_REORDERED", lang)));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse(string.Format(loc.T("ERR_FUNCTION_REORDER_FAILED", lang), ex.Message), "FUNCTION_REORDER_FAILED"));
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

            var exportData = new FunctionExportResponseDto
            {
                Version = "1.0",
                ExportDate = DateTime.UtcNow,
                Functions = rootNodes.Select(node => BuildExportNode(node, lookup)).ToList()
            };

            return Results.Ok(new SuccessResponse<FunctionExportResponseDto>(exportData));
        }).RequireFunction("SYS.SET.MENU");

        group.MapPost("/functions/import", async (
            [FromBody] MenuImportRequest request,
            [FromServices] AppDbContext db,
            [FromServices] AuditTrailService auditTrail,
            [FromServices] ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
            try
            {
                // 检查冲突
                var existingCodesList = await db.FunctionNodes
                    .AsNoTracking()
                    .Select(f => f.Code)
                    .ToListAsync(ct);
                var existingCodes = new HashSet<string>(existingCodesList);

                var importCodes = ExtractAllCodes(request.Functions);
                var conflicts = importCodes.Where(code => existingCodes.Contains(code)).ToList();

                if (conflicts.Count > 0 && request.MergeStrategy != "replace")
                {
                    return Results.BadRequest(new ErrorResponse(
                        loc.T("ERR_FUNCTION_CODE_CONFLICT", lang),
                        new Dictionary<string, string[]>
                        {
                            { "Conflicts", conflicts.ToArray() },
                            { "Hint", new[] { loc.T("ERR_FUNCTION_CODE_CONFLICT_HINT", lang) } }
                        },
                        "FUNCTION_CODE_CONFLICT"));
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

                var payload = new FunctionImportResultDto
                {
                    Message = string.Format(loc.T("MSG_FUNCTION_IMPORT_SUCCESS", lang), importedCount, skippedCount),
                    Imported = importedCount,
                    Skipped = skippedCount
                };

                return Results.Ok(new SuccessResponse<FunctionImportResultDto>(payload));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ErrorResponse(string.Format(loc.T("ERR_FUNCTION_IMPORT_FAILED", lang), ex.Message), "FUNCTION_IMPORT_FAILED"));
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
            return Results.Ok(new SuccessResponse<List<RoleProfileDto>>(roles.Select(ToRoleDto).ToList()));
        }).RequireFunction("BAS.AUTH.ROLE.PERM");

        group.MapPost("/roles", async ([FromBody] CreateRoleRequest request, [FromServices] AccessService service, CancellationToken ct) =>
        {
            var role = await service.CreateRoleAsync(request, ct);
            return Results.Ok(new SuccessResponse<RoleProfileDto>(ToRoleDto(role)));
        }).RequireFunction("BAS.AUTH.ROLE.PERM");

        group.MapGet("/roles/{roleId:guid}", async (Guid roleId, [FromServices] AppDbContext db, [FromServices] ILocalization loc, HttpContext http, CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
            var role = await db.RoleProfiles
                .AsNoTracking()
                .Include(r => r.Functions)
                .ThenInclude(f => f.Function)
                .Include(r => r.DataScopes)
                .FirstOrDefaultAsync(r => r.Id == roleId, ct);

            if (role == null)
                return Results.NotFound(new ErrorResponse(loc.T("ERR_ROLE_NOT_FOUND", lang), "ROLE_NOT_FOUND"));

            return Results.Ok(new SuccessResponse<RoleProfileDto>(ToRoleDto(role)));
        }).RequireFunction("BAS.AUTH.ROLE.PERM");

        group.MapPut("/roles/{roleId:guid}", async (Guid roleId, [FromBody] UpdateRoleRequest request, [FromServices] AppDbContext db, [FromServices] ILocalization loc, HttpContext http, CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
            var role = await db.RoleProfiles.FindAsync(new object[] { roleId }, ct);
            if (role == null)
                return Results.NotFound(new ErrorResponse(loc.T("ERR_ROLE_NOT_FOUND", lang), "ROLE_NOT_FOUND"));

            if (role.IsSystem)
                return Results.BadRequest(new ErrorResponse(loc.T("ERR_ROLE_SYSTEM_IMMUTABLE", lang), "ROLE_SYSTEM_IMMUTABLE"));

            if (!string.IsNullOrWhiteSpace(request.Name))
                role.Name = request.Name;
            if (request.Description is not null)
                role.Description = request.Description;
            if (request.IsEnabled.HasValue)
                role.IsEnabled = request.IsEnabled.Value;

            await db.SaveChangesAsync(ct);
            await db.Entry(role).Collection(r => r.Functions).LoadAsync(ct);
            await db.Entry(role).Collection(r => r.DataScopes).LoadAsync(ct);
            return Results.Ok(new SuccessResponse<RoleProfileDto>(ToRoleDto(role)));
        }).RequireFunction("BAS.AUTH.ROLE.PERM");

        group.MapDelete("/roles/{roleId:guid}", async (Guid roleId, [FromServices] AppDbContext db, [FromServices] ILocalization loc, HttpContext http, CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
            var role = await db.RoleProfiles
                .Include(r => r.Functions)
                .Include(r => r.DataScopes)
                .Include(r => r.Assignments)
                .FirstOrDefaultAsync(r => r.Id == roleId, ct);

            if (role == null)
                return Results.NotFound(new ErrorResponse(loc.T("ERR_ROLE_NOT_FOUND", lang), "ROLE_NOT_FOUND"));

            if (role.IsSystem)
                return Results.BadRequest(new ErrorResponse(loc.T("ERR_ROLE_SYSTEM_IMMUTABLE", lang), "ROLE_SYSTEM_IMMUTABLE"));

            if (role.Assignments?.Any() == true)
                return Results.BadRequest(new ErrorResponse(loc.T("ERR_ROLE_HAS_ASSIGNMENTS", lang), "ROLE_HAS_ASSIGNMENTS"));

            db.RoleProfiles.Remove(role);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponseExtensions.SuccessResponse());
        }).RequireFunction("BAS.AUTH.ROLE.PERM");

        group.MapPut("/roles/{roleId:guid}/permissions", async (Guid roleId, [FromBody] UpdatePermissionsRequest request, [FromServices] AppDbContext db, [FromServices] ILocalization loc, HttpContext http, CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
            var role = await db.RoleProfiles
                .Include(r => r.Functions)
                .Include(r => r.DataScopes)
                .FirstOrDefaultAsync(r => r.Id == roleId, ct);

            if (role == null)
                return Results.NotFound(new ErrorResponse(loc.T("ERR_ROLE_NOT_FOUND", lang), "ROLE_NOT_FOUND"));

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
            return Results.Ok(new SuccessResponse(loc.T("MSG_ROLE_PERMISSIONS_UPDATED", lang)));
        }).RequireFunction("BAS.AUTH.ROLE.PERM");

        group.MapPost("/assignments", async ([FromBody] AssignRoleRequest request, [FromServices] AccessService service, CancellationToken ct) =>
        {
            var assignment = await service.AssignRoleAsync(request, ct);
            return Results.Ok(new SuccessResponse<RoleAssignment>(assignment));
        }).RequireFunction("BAS.AUTH.USER.ROLE");

        group.MapGet("/assignments/user/{userId}", async (string userId, [FromServices] AppDbContext db, CancellationToken ct) =>
        {
            var assignments = await db.RoleAssignments
                .AsNoTracking()
                .Include(a => a.Role)
                .Where(a => a.UserId == userId)
                .Select(a => new RoleAssignmentDto
                {
                    Id = a.Id,
                    RoleId = a.RoleId,
                    RoleCode = a.Role!.Code,
                    RoleName = a.Role!.Name,
                    OrganizationId = a.OrganizationId,
                    ValidFrom = a.ValidFrom,
                    ValidTo = a.ValidTo
                })
                .ToListAsync(ct);

            return Results.Ok(new SuccessResponse<List<RoleAssignmentDto>>(assignments));
        }).RequireFunction("BAS.AUTH.USER.ROLE");

        group.MapDelete("/assignments/{assignmentId:guid}", async (Guid assignmentId, [FromServices] AppDbContext db, [FromServices] ILocalization loc, HttpContext http, CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
            var assignment = await db.RoleAssignments.FindAsync(new object[] { assignmentId }, ct);
            if (assignment == null)
                return Results.NotFound(new ErrorResponse(loc.T("ERR_ASSIGNMENT_NOT_FOUND", lang), "ASSIGNMENT_NOT_FOUND"));

            db.RoleAssignments.Remove(assignment);
            await db.SaveChangesAsync(ct);
            return Results.Ok(ApiResponseExtensions.SuccessResponse());
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

    private static async Task<FunctionNodeDto> ToDtoAsync(
        FunctionNode node,
        FunctionTreeBuilder treeBuilder,
        string? lang,
        CancellationToken ct)
    {
        var tree = await treeBuilder.BuildAsync(new[] { node }, lang: lang, ct: ct);
        return tree[0];
    }

    private static FunctionExportNodeDto BuildExportNode(FunctionNode node, Dictionary<Guid, FunctionNode> lookup)
    {
        var children = lookup.Values
            .Where(n => n.ParentId == node.Id)
            .OrderBy(n => n.SortOrder)
            .Select(child => BuildExportNode(child, lookup))
            .ToList();

        return new FunctionExportNodeDto
        {
            Code = node.Code,
            Name = node.Name,
            DisplayName = node.DisplayName,
            Route = node.Route,
            Icon = node.Icon,
            IsMenu = node.IsMenu,
            SortOrder = node.SortOrder,
            Children = children.Count > 0 ? children : null
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
