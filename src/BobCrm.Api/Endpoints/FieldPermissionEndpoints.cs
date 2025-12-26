using System.Security.Claims;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.Responses.Permissions;
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
            return Results.Ok(new SuccessResponse<List<FieldPermission>>(permissions));
        })
        .WithName("GetPermissionsByRole")
        .WithSummary("获取角色的所有字段权限")
        .Produces<SuccessResponse<List<FieldPermission>>>(StatusCodes.Status200OK);

        // Get field permissions by role and entity
        group.MapGet("/role/{roleId:guid}/entity/{entityType}", async (
            Guid roleId,
            string entityType,
            IFieldPermissionService service) =>
        {
            var permissions = await service.GetPermissionsByRoleAndEntityAsync(roleId, entityType);
            return Results.Ok(new SuccessResponse<List<FieldPermission>>(permissions));
        })
        .WithName("GetPermissionsByRoleAndEntity")
        .WithSummary("获取角色在指定实体的字段权限")
        .Produces<SuccessResponse<List<FieldPermission>>>(StatusCodes.Status200OK);

        // Get current user's field permission
        group.MapGet("/user/entity/{entityType}/field/{fieldName}", async (
            string entityType,
            string fieldName,
            ClaimsPrincipal user,
            IFieldPermissionService service) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var permission = await service.GetUserFieldPermissionAsync(userId, entityType, fieldName);
            return Results.Ok(new SuccessResponse<FieldPermission?>(permission));
        })
        .WithName("GetUserFieldPermission")
        .WithSummary("获取当前用户在指定字段的权限")
        .Produces<SuccessResponse<FieldPermission?>>(StatusCodes.Status200OK);

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

            return Results.Ok(new SuccessResponse<FieldPermission>(permission));
        })
        .WithName("UpsertFieldPermission")
        .WithSummary("创建或更新字段权限")
        .Produces<SuccessResponse<FieldPermission>>(StatusCodes.Status200OK);

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
        .WithSummary("批量设置角色字段权限")
        .Produces<SuccessResponse>(StatusCodes.Status200OK);

        // Delete a field permission
        group.MapDelete("/{permissionId:int}", async (
            int permissionId,
            IFieldPermissionService service,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            await service.DeletePermissionAsync(permissionId);
            return Results.Ok(new SuccessResponse(loc.T("MSG_FIELD_PERMISSION_DELETED", lang)));
        })
        .WithName("DeleteFieldPermission")
        .WithSummary("删除字段权限")
        .Produces<SuccessResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

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
        .WithSummary("删除角色的所有字段权限")
        .Produces<SuccessResponse>(StatusCodes.Status200OK);

        // Check if current user can read field
        group.MapGet("/user/entity/{entityType}/field/{fieldName}/can-read", async (
            string entityType,
            string fieldName,
            ClaimsPrincipal user,
            IFieldPermissionService service) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var canRead = await service.CanUserReadFieldAsync(userId, entityType, fieldName);
            return Results.Ok(new SuccessResponse<FieldPermissionCheckDto>(new FieldPermissionCheckDto { Allowed = canRead }));
        })
        .WithName("CanUserReadField")
        .WithSummary("检查当前用户是否可读该字段")
        .Produces<SuccessResponse<FieldPermissionCheckDto>>(StatusCodes.Status200OK);

        // Check if current user can write field
        group.MapGet("/user/entity/{entityType}/field/{fieldName}/can-write", async (
            string entityType,
            string fieldName,
            ClaimsPrincipal user,
            IFieldPermissionService service) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var canWrite = await service.CanUserWriteFieldAsync(userId, entityType, fieldName);
            return Results.Ok(new SuccessResponse<FieldPermissionCheckDto>(new FieldPermissionCheckDto { Allowed = canWrite }));
        })
        .WithName("CanUserWriteField")
        .WithSummary("检查当前用户是否可写该字段")
        .Produces<SuccessResponse<FieldPermissionCheckDto>>(StatusCodes.Status200OK);

        // Get readable fields for current user
        group.MapGet("/user/entity/{entityType}/readable-fields", async (
            string entityType,
            ClaimsPrincipal user,
            IFieldPermissionService service) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var fields = await service.GetReadableFieldsAsync(userId, entityType);
            return Results.Ok(new SuccessResponse<List<string>>(fields));
        })
        .WithName("GetReadableFields")
        .WithSummary("获取当前用户可读的所有字段")
        .Produces<SuccessResponse<List<string>>>(StatusCodes.Status200OK);

        // Get writable fields for current user
        group.MapGet("/user/entity/{entityType}/writable-fields", async (
            string entityType,
            ClaimsPrincipal user,
            IFieldPermissionService service) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var fields = await service.GetWritableFieldsAsync(userId, entityType);
            return Results.Ok(new SuccessResponse<List<string>>(fields));
        })
        .WithName("GetWritableFields")
        .WithSummary("获取当前用户可写的所有字段")
        .Produces<SuccessResponse<List<string>>>(StatusCodes.Status200OK);

        return app;
    }
}
