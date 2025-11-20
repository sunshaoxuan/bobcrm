using System.Security.Claims;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// 字段级权限管理端点
/// </summary>
public static class FieldPermissionEndpoints
{
    public static IEndpointRouteBuilder MapFieldPermissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/field-permissions")
            .WithTags("字段权限")
            .WithOpenApi()
            .RequireAuthorization();

        // 获取角色的所有字段权限
        group.MapGet("/role/{roleId:guid}", async (
            Guid roleId,
            IFieldPermissionService service) =>
        {
            var permissions = await service.GetPermissionsByRoleAsync(roleId);
            return Results.Ok(permissions);
        })
        .WithName("GetPermissionsByRole")
        .WithSummary("获取角色的所有字段权限");

        // 获取角色对特定实体的字段权限
        group.MapGet("/role/{roleId:guid}/entity/{entityType}", async (
            Guid roleId,
            string entityType,
            IFieldPermissionService service) =>
        {
            var permissions = await service.GetPermissionsByRoleAndEntityAsync(roleId, entityType);
            return Results.Ok(permissions);
        })
        .WithName("GetPermissionsByRoleAndEntity")
        .WithSummary("获取角色对特定实体的字段权限");

        // 获取当前用户对特定实体字段的权限
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
        .WithSummary("获取当前用户对特定实体字段的权限");

        // 创建或更新字段权限
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
        .WithSummary("创建或更新字段权限");

        // 批量设置角色的字段权限
        group.MapPost("/role/{roleId:guid}/entity/{entityType}/bulk", async (
            Guid roleId,
            string entityType,
            BulkUpsertFieldPermissionsRequest request,
            ClaimsPrincipal user,
            IFieldPermissionService service) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            await service.BulkUpsertPermissionsAsync(roleId, entityType, request.Permissions, userId);
            return Results.Ok(new { message = "Bulk upsert completed successfully" });
        })
        .WithName("BulkUpsertFieldPermissions")
        .WithSummary("批量设置角色的字段权限");

        // 删除字段权限
        group.MapDelete("/{permissionId:int}", async (
            int permissionId,
            IFieldPermissionService service) =>
        {
            try
            {
                await service.DeletePermissionAsync(permissionId);
                return Results.Ok(new { message = "Permission deleted successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .WithName("DeleteFieldPermission")
        .WithSummary("删除字段权限");

        // 删除角色的所有字段权限
        group.MapDelete("/role/{roleId:guid}", async (
            Guid roleId,
            IFieldPermissionService service) =>
        {
            await service.DeletePermissionsByRoleAsync(roleId);
            return Results.Ok(new { message = "All permissions deleted successfully" });
        })
        .WithName("DeletePermissionsByRole")
        .WithSummary("删除角色的所有字段权限");

        // 检查当前用户是否可以读取字段
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
        .WithSummary("检查当前用户是否可以读取字段");

        // 检查当前用户是否可以写入字段
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
        .WithSummary("检查当前用户是否可以写入字段");

        // 获取当前用户对实体的所有可读字段列表
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
        .WithSummary("获取当前用户对实体的所有可读字段列表");

        // 获取当前用户对实体的所有可写字段列表
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
        .WithSummary("获取当前用户对实体的所有可写字段列表");

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
