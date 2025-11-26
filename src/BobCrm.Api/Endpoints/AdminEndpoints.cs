using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Base;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Services;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// 管理和调试相关端点
/// </summary>
public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin")
            .WithTags("管理员")
            .WithOpenApi()
            .WithDescription("仅限开发环境使用");

        var debugGroup = app.MapGroup("/api/debug")
            .WithTags("调试")
            .WithOpenApi()
            .WithDescription("仅限开发环境使用");

        // 数据库健康检查
        group.MapGet("/db/health", async (
            AppDbContext db,
            ILogger<Program> logger) =>
        {
            logger.LogInformation("[Admin] Database health check requested");
            
            var provider = db.Database.ProviderName ?? "unknown";
            var canConnect = await db.Database.CanConnectAsync();
            var info = new
            {
                provider,
                canConnect,
                counts = new
                {
                    customers = await db.Customers.CountAsync(),
                    fieldDefinitions = await db.FieldDefinitions.CountAsync(),
                    fieldValues = await db.FieldValues.CountAsync(),
                    userLayouts = await db.UserLayouts.CountAsync()
                }
            };
            
            logger.LogInformation("[Admin] Health check result: provider={Provider}, canConnect={CanConnect}", provider, canConnect);
            return Results.Json(info);
        })
        .WithName("DbHealthCheck")
        .WithSummary("数据库健康检查")
        .WithDescription("检查数据库连接状态和记录数量（仅开发环境）");

        // 重建数据库
        group.MapPost("/db/recreate", async (
            AppDbContext db,
            ILogger<Program> logger) =>
        {
            logger.LogWarning("[Admin] Database recreation requested - this will delete all data!");
            await DatabaseInitializer.RecreateAsync(db);
            logger.LogInformation("[Admin] Database recreated successfully");
            
            return Results.Ok(ApiResponseExtensions.SuccessResponse("数据库已重建"));
        })
        .WithName("RecreateDatabase")
        .WithSummary("重建数据库")
        .WithDescription("删除并重建整个数据库（仅开发环境，危险操作）");

        // 调试：列出所有用户
        debugGroup.MapGet("/users", async (
            UserManager<IdentityUser> um,
            ILogger<Program> logger) =>
        {
            logger.LogInformation("[Debug] User list requested");
            
            var userList = new List<object>();
            foreach (var u in um.Users)
            {
                var hasPassword = await um.HasPasswordAsync(u);
                userList.Add(new { id = u.Id, username = u.UserName, email = u.Email, emailConfirmed = u.EmailConfirmed, hasPassword });
            }
            
            logger.LogInformation("[Debug] Retrieved {Count} users", userList.Count);
            return Results.Ok(userList);
        })
        .WithName("DebugListUsers")
        .WithSummary("列出所有用户")
        .WithDescription("调试用：列出系统中的所有用户（仅开发环境）");

        // 调试：重置初始化
        debugGroup.MapPost("/reset-setup", async (
            UserManager<IdentityUser> um,
            RoleManager<IdentityRole> rm,
            ILogger<Program> logger) =>
        {
            logger.LogWarning("[Debug] Setup reset requested");
            
            var admin = await um.FindByNameAsync("admin");
            if (admin != null)
            {
                var result = await um.DeleteAsync(admin);
                if (result.Succeeded)
                {
                    logger.LogInformation("[Debug] Admin user deleted successfully");
                    return Results.Ok(ApiResponseExtensions.SuccessResponse("管理员用户已删除，可以重新初始化"));
                }
                else
                {
                    logger.LogError("[Debug] Failed to delete admin user: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
                    return Results.BadRequest(new { error = "Failed to delete admin user", details = string.Join("; ", result.Errors.Select(e => e.Description)) });
                }
            }

            logger.LogInformation("[Debug] No admin user found");
            return Results.Ok(ApiResponseExtensions.SuccessResponse("未找到管理员用户，可以进行初始化"));
        })
        .WithName("DebugResetSetup")
        .WithSummary("重置初始化")
        .WithDescription("删除管理员用户，允许重新初始化系统（仅开发环境）");

        // 调试：重置管理员密码
        group.MapPost("/reset-password", async (
            UserManager<IdentityUser> um,
            RoleManager<IdentityRole> rm,
            ResetPasswordDto dto,
            ILogger<Program> logger) =>
        {
            if (!await rm.RoleExistsAsync("admin"))
            {
                logger.LogWarning("[Admin] Admin role not found");
                return Results.NotFound(new { error = "admin role not found" });
            }

            var admins = await um.GetUsersInRoleAsync("admin");
            var user = admins.FirstOrDefault();
            if (user == null)
            {
                logger.LogWarning("[Admin] Admin user not found");
                return Results.NotFound(new { error = "admin user not found" });
            }

            logger.LogWarning("[Admin] Resetting password for admin user {UserName}", user.UserName);

            if (await um.HasPasswordAsync(user))
            {
                var rmv = await um.RemovePasswordAsync(user);
                if (!rmv.Succeeded)
                {
                    logger.LogError("[Admin] Failed to remove old password: {Errors}", string.Join("; ", rmv.Errors.Select(e => e.Description)));
                    return Results.BadRequest(new { error = string.Join("; ", rmv.Errors.Select(e => e.Description)) });
                }
            }

            var add = await um.AddPasswordAsync(user, dto.NewPassword);
            if (!add.Succeeded)
            {
                logger.LogError("[Admin] Failed to set new password: {Errors}", string.Join("; ", add.Errors.Select(e => e.Description)));
                return Results.BadRequest(new { error = string.Join("; ", add.Errors.Select(e => e.Description)) });
            }

            user.EmailConfirmed = true;
            await um.UpdateAsync(user);
            
            logger.LogInformation("[Admin] Password reset successfully for user {UserName}", user.UserName);
            return Results.Ok(new { status = "ok", user = new { user = user.UserName, email = user.Email } });
        })
        .WithName("AdminResetPassword")
        .WithSummary("重置管理员密码")
        .WithDescription("重置管理员账户密码（仅开发环境）");

        // 重新生成默认模板（系统/现有实体）
        group.MapPost("/templates/regenerate-defaults", async (
            AppDbContext db,
            IDefaultTemplateService templateService,
            TemplateBindingService bindingService,
            ILogger<Program> logger) =>
        {
            logger.LogInformation("[Admin] Regenerate-defaults called for all entities");
            var entities = await db.EntityDefinitions
                .Include(e => e.Fields)
                .AsNoTracking()
                .ToListAsync();

            var updated = 0;
            foreach (var entity in entities)
            {
                var result = await templateService.EnsureTemplatesAsync(entity, "admin", force: true);
                foreach (var kv in result.Templates)
                {
                    await bindingService.UpsertBindingAsync(
                        entity.EntityRoute ?? entity.EntityName,
                        kv.Key,
                        kv.Value.Id,
                        isSystem: true,
                        updatedBy: "admin",
                        requiredFunctionCode: null);
                }

                updated += result.Created.Count + result.Updated.Count;
            }

            logger.LogInformation("[Admin] Regenerated default templates for {Count} entities, changes={Updated}", entities.Count, updated);
            return Results.Ok(new { entities = entities.Count, updated });
        })
        .WithName("RegenerateDefaultTemplates")
        .WithSummary("重新生成默认模板")
        .WithDescription("为所有实体重新生成系统默认模板，并更新绑定（仅开发环境）");

        // 针对指定实体重新生成默认模板
        group.MapPost("/templates/{entityRoute}/regenerate", async (
            string entityRoute,
            AppDbContext db,
            IDefaultTemplateService templateService,
            TemplateBindingService bindingService,
            ILogger<Program> logger) =>
        {
            var normalized = entityRoute?.Trim() ?? string.Empty;
            var normalizedLower = normalized.ToLowerInvariant();
            logger.LogInformation("[Admin] Regenerate defaults requested for entityRoute={EntityRoute}", normalized);

            // Accept entityRoute, EntityName, or fully-qualified Namespace.EntityName (all case-insensitive)
            var entity = await db.EntityDefinitions
                .Include(e => e.Fields)
                .FirstOrDefaultAsync(e =>
                    (e.EntityRoute ?? string.Empty).ToLower() == normalizedLower ||
                    (e.EntityName ?? string.Empty).ToLower() == normalizedLower ||
                    ((e.Namespace ?? string.Empty) + "." + (e.EntityName ?? string.Empty)).ToLower() == normalizedLower);

            if (entity == null)
            {
                var available = await db.EntityDefinitions
                    .Select(e => new { e.EntityRoute, e.EntityName })
                    .ToListAsync();
                logger.LogWarning("[Admin] Regenerate failed: entity {EntityRoute} not found. Available: {Available}",
                    normalized,
                    string.Join(", ", available.Select(a => a.EntityRoute ?? a.EntityName ?? string.Empty)));
                return Results.NotFound(new { error = $"Entity '{normalized}' not found" });
            }

            var result = await templateService.EnsureTemplatesAsync(entity, "admin", force: true);
            foreach (var kv in result.Templates)
            {
                await bindingService.UpsertBindingAsync(
                    entity.EntityRoute ?? entity.EntityName,
                    kv.Key,
                    kv.Value.Id,
                    isSystem: true,
                    updatedBy: "admin",
                    requiredFunctionCode: null);
            }

            logger.LogInformation("[Admin] Regenerated templates for {Entity} created={Created} updated={Updated}",
                normalized, result.Created.Count, result.Updated.Count);

            return Results.Ok(new
            {
                entity = normalized,
                created = result.Created.Count,
                updated = result.Updated.Count
            });
        })
        .WithName("RegenerateDefaultTemplatesForEntity")
        .WithSummary("为指定实体重新生成默认模板")
        .WithDescription("仅开发环境，管理员可对单个实体重新生成系统默认模板并更新绑定。");

        return app;
    }
}
