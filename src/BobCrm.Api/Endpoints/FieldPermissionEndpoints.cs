using System.Security.Claims;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts;
using BobCrm.Api.Infrastructure;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// Field-level permission endpoints
/// </summary>
public static class FieldPermissionEndpoints
{
    public static IEndpointRouteBuilder MapFieldPermissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/field-permissions")
            .WithTags("FieldPermissions")
            .WithOpenApi()
            .RequireAuthorization();

        // Get all field permissions for a role
        group.MapGet("/role/{roleId:guid}", async (
            Guid roleId,
            IFieldPermissionService service) =>
        {
            var permissions = await service.GetPermissionsByRoleAsync(roleId);
            return Results.Ok(permissions);
        })
        .WithName("GetPermissionsByRole")
        .WithSummary("Get all field permissions for a role");

        // Get field permissions by role and entity
        group.MapGet("/role/{roleId:guid}/entity/{entityType}", async (
            Guid roleId,
            string entityType,
            IFieldPermissionService service) =>
        {
            var permissions = await service.GetPermissionsByRoleAndEntityAsync(roleId, entityType);
            return Results.Ok(permissions);
        })
        .WithName("GetPermissionsByRoleAndEntity")
        .WithSummary("Get field permissions of a role for an entity");

        // Get current user's field permission
        group.MapGet("/user/entity/{entityType}/field/{fieldName}", async (
            string entityType,
            string fieldName,
            ClaimsPrincipal user,
            IFieldPermissionService service) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var permission = await service.GetUserFieldPermissionAsync(userId, entityType, fieldName);
            return Results.Ok(permission);
        })
        .WithName("GetUserFieldPermission")
        .WithSummary("Get current user's permission for a field");

        // Create or update a field permission
        group.MapPost("/role/{roleId:guid}/entity/{entityType}/field/{fieldName}", async (
            Guid roleId,
            string entityType,
            string fieldName,
            UpsertFieldPermissionRequest request,
            ClaimsPrincipal user,
            IFieldPermissionService service) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var permission = await service.UpsertPermissionAsync(
                roleId,
                entityType,
                fieldName,
                request.CanRead,
                request.CanWrite,
                request.Remarks,
                userId);

            return Results.Ok(permission);
        })
        .WithName("UpsertFieldPermission")
        .WithSummary("Create or update a field permission");

        // Bulk upsert field permissions
        group.MapPost("/role/{roleId:guid}/entity/{entityType}/bulk", async (
            Guid roleId,
            string entityType,
            BulkUpsertFieldPermissionsRequest request,
            ClaimsPrincipal user,
            IFieldPermissionService service,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            await service.BulkUpsertPermissionsAsync(roleId, entityType, request.Permissions, userId);
            return Results.Ok(new SuccessResponse(loc.T("MSG_FIELD_PERMISSIONS_BULK_UPSERT", lang)));
        })
        .WithName("BulkUpsertFieldPermissions")
        .WithSummary("Bulk set field permissions for a role");

        // Delete a field permission
        group.MapDelete("/{permissionId:int}", async (
            int permissionId,
            IFieldPermissionService service,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            try
            {
                await service.DeletePermissionAsync(permissionId);
                return Results.Ok(new SuccessResponse(loc.T("MSG_FIELD_PERMISSION_DELETED", lang)));
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new ErrorResponse(ex.Message, "FIELD_PERMISSION_NOT_FOUND"));
            }
        })
        .WithName("DeleteFieldPermission")
        .WithSummary("Delete a field permission");

        // Delete all field permissions for a role
        group.MapDelete("/role/{roleId:guid}", async (
            Guid roleId,
            IFieldPermissionService service,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            await service.DeletePermissionsByRoleAsync(roleId);
            return Results.Ok(new SuccessResponse(loc.T("MSG_FIELD_PERMISSIONS_DELETED_ALL", lang)));
        })
        .WithName("DeletePermissionsByRole")
        .WithSummary("Delete all field permissions for a role");

        // Check if current user can read field
        group.MapGet("/user/entity/{entityType}/field/{fieldName}/can-read", async (
            string entityType,
            string fieldName,
            ClaimsPrincipal user,
            IFieldPermissionService service) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var canRead = await service.CanUserReadFieldAsync(userId, entityType, fieldName);
            return Results.Ok(new { canRead });
        })
        .WithName("CanUserReadField")
        .WithSummary("Check if current user can read the field");

        // Check if current user can write field
        group.MapGet("/user/entity/{entityType}/field/{fieldName}/can-write", async (
            string entityType,
            string fieldName,
            ClaimsPrincipal user,
            IFieldPermissionService service) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var canWrite = await service.CanUserWriteFieldAsync(userId, entityType, fieldName);
            return Results.Ok(new { canWrite });
        })
        .WithName("CanUserWriteField")
        .WithSummary("Check if current user can write the field");

        // Get readable fields for current user
        group.MapGet("/user/entity/{entityType}/readable-fields", async (
            string entityType,
            ClaimsPrincipal user,
            IFieldPermissionService service) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var fields = await service.GetReadableFieldsAsync(userId, entityType);
            return Results.Ok(fields);
        })
        .WithName("GetReadableFields")
        .WithSummary("Get all readable fields for current user");

        // Get writable fields for current user
        group.MapGet("/user/entity/{entityType}/writable-fields", async (
            string entityType,
            ClaimsPrincipal user,
            IFieldPermissionService service) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var fields = await service.GetWritableFieldsAsync(userId, entityType);
            return Results.Ok(fields);
        })
        .WithName("GetWritableFields")
        .WithSummary("Get all writable fields for current user");

        return app;
    }
}

// 请求DTO
public record UpsertFieldPermissionRequest(
    bool CanRead,
    bool CanWrite,
    string? Remarks = null);

public record BulkUpsertFieldPermissionsRequest(
    List<FieldPermissionDto> Permissions);
