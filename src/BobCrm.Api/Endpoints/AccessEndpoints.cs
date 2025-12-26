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
            [FromServices] FunctionService functionService,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Results.Unauthorized();
            }

            var targetLang = LangHelper.GetLang(http, lang);
            var tree = await functionService.GetMyFunctionsAsync(userId, targetLang, ct);
            return Results.Ok(new SuccessResponse<List<FunctionNodeDto>>(tree));
        });

        group.MapPost("/functions", async ([FromBody] CreateFunctionRequest request,
            [FromQuery] string? lang,
            [FromServices] FunctionService service,
            [FromServices] FunctionTreeBuilder treeBuilder,
            [FromServices] AuditTrailService auditTrail,
            [FromServices] ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            var uiLang = LangHelper.GetLang(http);
            var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
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
        }).RequireFunction("SYS.SET.MENU");

        group.MapPut("/functions/{id:guid}", async (Guid id,
            [FromBody] UpdateFunctionRequest request,
            [FromQuery] string? lang,
            [FromServices] FunctionService service,
            [FromServices] FunctionTreeBuilder treeBuilder,
            [FromServices] AuditTrailService auditTrail,
            [FromServices] ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            var uiLang = LangHelper.GetLang(http);
            var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
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
        }).RequireFunction("SYS.SET.MENU");

        group.MapDelete("/functions/{id:guid}", async (Guid id,
            [FromServices] FunctionService service,
            [FromServices] AuditTrailService auditTrail,
            [FromServices] ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
                await service.DeleteFunctionAsync(id, ct);
                await auditTrail.RecordAsync("MENU", "DELETE", $"Deleted function {id}", id.ToString(), null, ct);
                return Results.Ok(ApiResponseExtensions.SuccessResponse(loc.T("MSG_FUNCTION_DELETED", lang)));
        }).RequireFunction("SYS.SET.MENU");

        group.MapPost("/functions/reorder", async ([FromBody] List<FunctionOrderUpdate> updates,
            [FromServices] FunctionService service,
            [FromServices] AuditTrailService auditTrail,
            [FromServices] ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
                await service.ReorderFunctionsAsync(updates, ct);
                await auditTrail.RecordAsync("MENU", "REORDER", "Reordered menu nodes", null, updates, ct);
                return Results.Ok(new SuccessResponse(loc.T("MSG_FUNCTIONS_REORDERED", lang)));
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
            [FromServices] FunctionService service,
            [FromServices] AuditTrailService auditTrail,
            [FromServices] ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
                var (importedCount, skippedCount) = await service.ImportFunctionsAsync(request, ct);

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

        group.MapPost("/roles", async ([FromBody] CreateRoleRequest request, [FromServices] RoleService service, CancellationToken ct) =>
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

        group.MapPut("/roles/{roleId:guid}", async (Guid roleId, [FromBody] UpdateRoleRequest request, [FromServices] RoleService service, [FromServices] ILocalization loc, HttpContext http, CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
                var role = await service.UpdateRoleAsync(roleId, request, ct);
                return Results.Ok(new SuccessResponse<RoleProfileDto>(ToRoleDto(role)));
        }).RequireFunction("BAS.AUTH.ROLE.PERM");

        group.MapDelete("/roles/{roleId:guid}", async (Guid roleId, [FromServices] RoleService service, [FromServices] ILocalization loc, HttpContext http, CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
                await service.DeleteRoleAsync(roleId, ct);
                return Results.Ok(ApiResponseExtensions.SuccessResponse());
        }).RequireFunction("BAS.AUTH.ROLE.PERM");

        group.MapPut("/roles/{roleId:guid}/permissions", async (Guid roleId, [FromBody] UpdatePermissionsRequest request, [FromServices] RoleService service, [FromServices] ILocalization loc, HttpContext http, CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
                await service.UpdateRolePermissionsAsync(roleId, request, ct);
                return Results.Ok(new SuccessResponse(loc.T("MSG_ROLE_PERMISSIONS_UPDATED", lang)));
        }).RequireFunction("BAS.AUTH.ROLE.PERM");

        group.MapPost("/assignments", async ([FromBody] AssignRoleRequest request, [FromServices] RoleService service, CancellationToken ct) =>
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

        group.MapDelete("/assignments/{assignmentId:guid}", async (Guid assignmentId, [FromServices] RoleService service, [FromServices] ILocalization loc, HttpContext http, CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
                await service.DeleteAssignmentAsync(assignmentId, ct);
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



}
