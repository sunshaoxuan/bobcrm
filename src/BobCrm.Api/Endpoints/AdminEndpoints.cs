using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// 管理和调试相关端点
/// </summary>
public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var docLang = ResolveDocLanguage(app);
        string Doc(string key) => ResolveDocString(app, docLang, key);

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
            ILogger<Program> logger,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            logger.LogWarning("[Admin] Database recreation requested - this will delete all data!");
            await DatabaseInitializer.RecreateAsync(db);
            logger.LogInformation("[Admin] Database recreated successfully");
            
            return Results.Ok(ApiResponseExtensions.SuccessResponse(loc.T("MSG_DB_RECREATED", lang)));
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
            ILogger<Program> logger,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            logger.LogWarning("[Debug] Setup reset requested");
            
            var admin = await um.FindByNameAsync("admin");
            if (admin != null)
            {
                var result = await um.DeleteAsync(admin);
                if (result.Succeeded)
                {
                    logger.LogInformation("[Debug] Admin user deleted successfully");
                    return Results.Ok(ApiResponseExtensions.SuccessResponse(loc.T("MSG_ADMIN_DELETED", lang)));
                }
                else
                {
                    logger.LogError("[Debug] Failed to delete admin user: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
                    return Results.BadRequest(new ErrorResponse(loc.T("ERR_ADMIN_DELETE_FAILED", lang), result.Errors.ToDictionary(e => e.Code, e => new[] { e.Description })));
                }
            }

            logger.LogInformation("[Debug] No admin user found");
            return Results.Ok(ApiResponseExtensions.SuccessResponse(loc.T("MSG_ADMIN_NOT_FOUND_INIT", lang)));
        })
        .WithName("DebugResetSetup")
        .WithSummary("重置初始化")
        .WithDescription("删除管理员用户，允许重新初始化系统（仅开发环境）");

        // 调试：重置管理员密码
        group.MapPost("/reset-password", async (
            UserManager<IdentityUser> um,
            RoleManager<IdentityRole> rm,
            ResetPasswordDto dto,
            ILogger<Program> logger,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            if (!await rm.RoleExistsAsync("admin"))
            {
                logger.LogWarning("[Admin] Admin role not found");
                return Results.NotFound(new ErrorResponse(loc.T("ERR_ADMIN_ROLE_NOT_FOUND", lang), "ADMIN_ROLE_NOT_FOUND"));
            }

            var admins = await um.GetUsersInRoleAsync("admin");
            var user = admins.FirstOrDefault();
            if (user == null)
            {
                logger.LogWarning("[Admin] Admin user not found");
                return Results.NotFound(new ErrorResponse(loc.T("ERR_ADMIN_USER_NOT_FOUND", lang), "ADMIN_USER_NOT_FOUND"));
            }

            logger.LogWarning("[Admin] Resetting password for admin user {UserName}", user.UserName);

            if (await um.HasPasswordAsync(user))
            {
                var rmv = await um.RemovePasswordAsync(user);
                if (!rmv.Succeeded)
                {
                    logger.LogError("[Admin] Failed to remove old password: {Errors}", string.Join("; ", rmv.Errors.Select(e => e.Description)));
                    return Results.BadRequest(new ErrorResponse(loc.T("ERR_ADMIN_REMOVE_PWD_FAILED", lang), rmv.Errors.ToDictionary(e => e.Code, e => new[] { e.Description })));
                }
            }

            var add = await um.AddPasswordAsync(user, dto.NewPassword);
            if (!add.Succeeded)
            {
                logger.LogError("[Admin] Failed to set new password: {Errors}", string.Join("; ", add.Errors.Select(e => e.Description)));
                return Results.BadRequest(new ErrorResponse(loc.T("ERR_ADMIN_SET_PWD_FAILED", lang), add.Errors.ToDictionary(e => e.Code, e => new[] { e.Description })));
            }

            user.EmailConfirmed = true;
            await um.UpdateAsync(user);
            
            logger.LogInformation("[Admin] Password reset successfully for user {UserName}", user.UserName);
            return Results.Ok(new SuccessResponse(loc.T("MSG_ADMIN_PASSWORD_RESET_OK", lang)));
        })
        .WithName("AdminResetPassword")
        .WithSummary("重置管理员密码")
        .WithDescription("重置管理员账户密码（仅开发环境）");

        // 重新生成默认模板（系统/现有实体）
        group.MapPost("/templates/regenerate-defaults", async (
            AppDbContext db,
            IDefaultTemplateService templateService,
            TemplateBindingService bindingService,
            ILogger<Program> logger,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            logger.LogInformation("[Admin] Regenerate-defaults called for all entities");
            var entities = await db.EntityDefinitions
                .Include(e => e.Fields)
                .AsNoTracking()
                .ToListAsync();

            var updated = 0;
            foreach (var entity in entities)
            {
                var result = await templateService.EnsureTemplatesAsync(entity, "admin", force: true);



                await db.SaveChangesAsync();
                updated += result.Created.Count + result.Updated.Count;
            }

            logger.LogInformation("[Admin] Regenerated default templates for {Count} entities, changes={Updated}", entities.Count, updated);
            return Results.Ok(new SuccessResponse<object>(new { entities = entities.Count, updated, message = loc.T("MSG_REGENERATE_DEFAULT_TEMPLATES", lang) }));
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
            ILogger<Program> logger,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
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
                return Results.NotFound(new ErrorResponse(loc.T("ERR_ENTITY_NOT_FOUND", lang), "ENTITY_NOT_FOUND"));
            }

            var result = await templateService.EnsureTemplatesAsync(entity, "admin", force: true);



            await db.SaveChangesAsync();

            logger.LogInformation("[Admin] Regenerated templates for {Entity} created={Created} updated={Updated}",
                normalized, result.Created.Count, result.Updated.Count);

            return Results.Ok(new SuccessResponse<object>(new
            {
                entity = normalized,
                created = result.Created.Count,
                updated = result.Updated.Count
            }));
        })
        .WithName("RegenerateDefaultTemplatesForEntity")
        .WithSummary("为指定实体重新生成默认模板")
        .WithDescription("仅开发环境，管理员可对单个实体重新生成系统默认模板并更新绑定。");

        // 硬重置指定实体的所有系统模板和绑定（用户模板保留，仅能手工管理）
        // 执行步骤：
        // 1. 删除实体的所有系统默认模板
        // 2. 删除关联的模板状态绑定（仅限系统模板）
        // 3. 删除旧的系统模板绑定记录
        // 4. 重新创建干净的系统默认模板
        // 此操作不可逆，请谨慎使用。
        group.MapPost("/templates/{entityRoute}/reset", async (
            string entityRoute,
            AppDbContext db,
            IDefaultTemplateService templateService,
            ILogger<Program> logger,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            // 规范化参数：FormTemplate.EntityType 存储的是 EntityDefinition.EntityRoute
            var normalizedEntityType = entityRoute?.Trim() ?? string.Empty;
            logger.LogInformation("[TemplateReset] Hard reset requested for entityType={EntityType}", normalizedEntityType);

            // 通用查询逻辑：EntityRoute/EntityName/FullName 均可，忽略大小写
            var normalizedLower = normalizedEntityType.ToLowerInvariant();
            var altNormalized = normalizedLower.EndsWith("s")
                ? normalizedLower.TrimEnd('s')
                : $"{normalizedLower}s";

            var entityQuery = db.EntityDefinitions
                .Include(e => e.Fields)
                .AsQueryable();

            var entity = await entityQuery.FirstOrDefaultAsync(e =>
                (e.EntityRoute ?? string.Empty).ToLower() == normalizedLower ||
                (e.EntityName ?? string.Empty).ToLower() == normalizedLower ||
                ((e.Namespace ?? string.Empty) + "." + (e.EntityName ?? string.Empty)).ToLower() == normalizedLower);

            if (entity == null && !string.IsNullOrWhiteSpace(altNormalized))
            {
                // 尝试单复数互换（如 user -> users, role -> roles）
                entity = await entityQuery.FirstOrDefaultAsync(e =>
                    (e.EntityRoute ?? string.Empty).ToLower() == altNormalized ||
                    (e.EntityName ?? string.Empty).ToLower() == altNormalized);
            }

            if (entity == null)
            {
                // 提供详细的诊断信息
                var availableEntities = await db.EntityDefinitions
                    .Select(e => new { e.EntityRoute, e.EntityName, e.Source })
                    .ToListAsync();

                logger.LogWarning(
                    "[TemplateReset] Entity not found. EntityType='{EntityType}', TriedAlt='{Alt}', Available: {Available}",
                    normalizedEntityType,
                    altNormalized,
                    string.Join(", ", availableEntities.Select(e => $"{e.EntityRoute}({e.EntityName},{e.Source})")));

                return Results.NotFound(new ErrorResponse(
                    loc.T("ERR_ENTITY_NOT_FOUND", lang),
                    new Dictionary<string, string[]>
                    {
                        { "Hint", new[] { loc.T("ERR_ENTITY_ROUTE_HINT", lang) } },
                        { "Requested", new[] { normalizedEntityType } },
                        { "Available", availableEntities.Select(e => e.EntityRoute ?? string.Empty).Distinct().ToArray() }
                    },
                    "ENTITY_NOT_FOUND"));
            }

            var entityType = entity.EntityRoute!; // EntityRoute is the canonical identifier

            try
            {
                var existingBefore = await db.FormTemplates
                    .Where(t => t.EntityType == entityType)
                    .CountAsync();
                logger.LogInformation("[TemplateReset] Starting reset for {Entity}. Existing templates: {Count}", entityType, existingBefore);

                // Remove ALL templates for this entity (system + user/test data) to ensure clean rebuild
                var allTemplates = await db.FormTemplates
                    .Where(t => t.EntityType == entityType)
                    .ToListAsync();

                db.FormTemplates.RemoveRange(allTemplates);
                logger.LogInformation("[TemplateReset] Deleting {Count} templates for {Entity}",
                    allTemplates.Count, entityType);

                // Delete bindings tied to this entity
                var stateBindings = await db.TemplateStateBindings
                    .Where(b => b.EntityType == entityType)
                    .ToListAsync();

                db.TemplateStateBindings.RemoveRange(stateBindings);
                logger.LogInformation("[TemplateReset] Deleting {Count} state bindings for {Entity}", 
                    stateBindings.Count, entityType);

                var legacyBindings = await db.TemplateBindings
                    .Where(b => b.EntityType == entityType)
                    .ToListAsync();

                db.TemplateBindings.RemoveRange(legacyBindings);
                logger.LogInformation("[TemplateReset] Deleting {Count} legacy bindings for {Entity}", 
                    legacyBindings.Count, entityType);

                await db.SaveChangesAsync();
                logger.LogInformation("[TemplateReset] System templates and bindings deleted for {Entity}", entityType);

                // Recreate fresh system default templates
                var result = await templateService.EnsureTemplatesAsync(entity, "admin", force: true);

                logger.LogInformation("[TemplateReset] Reset complete for {Entity}. Deleted={Deleted}, CreatedSystem={Created} templates",
                    normalizedEntityType, allTemplates.Count, result.Created.Count);

                // Fetch current templates to aid debugging (ids + updatedAt)
                var current = await db.FormTemplates
                    .Where(t => t.EntityType == entityType)
                    .Select(t => new { t.Id, t.Name, t.UsageType, t.IsSystemDefault, t.IsUserDefault, t.UserId, t.UpdatedAt })
                    .ToListAsync();
                if (current.Count == 0)
                {
                    logger.LogWarning("[TemplateReset] No templates found after reset for {Entity}", entityType);
                }
                else
                {
                    logger.LogInformation("[TemplateReset] Post-reset templates for {Entity}: {Templates}",
                        entityType,
                        string.Join(", ", current.Select(c =>
                            $"{c.Id}:{c.UsageType}:{(c.IsSystemDefault ? "sys" : c.IsUserDefault ? "user" : c.UserId)}:{c.Name}@{c.UpdatedAt:O}")));
                }

                return Results.Ok(new
                {
                    entity = normalizedEntityType,
                    deleted = allTemplates.Count,
                    deletedBindings = stateBindings.Count + legacyBindings.Count,
                    createdSystemTemplates = result.Created.Count,
                    createdTemplates = result.Templates.Keys.ToList(),
                    currentTemplates = current
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[TemplateReset] Failed to reset templates for {Entity}", normalizedEntityType);
                return Results.Problem($"Failed to reset templates: {ex.Message}");
            }
        })
        .WithName("ResetTemplatesForEntity")
        .WithSummary(Doc("ADMIN_TEMPLATE_RESET_SUMMARY"))
        .WithDescription(Doc("ADMIN_TEMPLATE_RESET_DESCRIPTION"));

        // 全量重置所有实体的模板（开发/测试专用）：删除所有模板与绑定，重新生成系统默认模板
        group.MapPost("/templates/reset-all", async (
            AppDbContext db,
            IDefaultTemplateService templateService,
            ILogger<Program> logger) =>
        {
            logger.LogWarning("[TemplateResetAll] Full reset of all templates requested (dev only)");

            // 1. 删除所有模板与绑定（包含用户模板；开发阶段允许清空）
            var allTemplates = await db.FormTemplates.ToListAsync();
            db.FormTemplates.RemoveRange(allTemplates);

            var stateBindings = await db.TemplateStateBindings.ToListAsync();
            db.TemplateStateBindings.RemoveRange(stateBindings);

            var legacyBindings = await db.TemplateBindings.ToListAsync();
            db.TemplateBindings.RemoveRange(legacyBindings);

            await db.SaveChangesAsync();

            logger.LogInformation("[TemplateResetAll] Deleted templates={Templates}, stateBindings={StateBindings}, legacyBindings={LegacyBindings}",
                allTemplates.Count, stateBindings.Count, legacyBindings.Count);

            // 2. 为所有实体重新生成系统默认模板
            var entities = await db.EntityDefinitions
                .Include(e => e.Fields)
                .ToListAsync();

            var created = 0;
            var updated = 0;
            var regeneratedEntities = new List<string>();

            foreach (var entity in entities)
            {
                var result = await templateService.EnsureTemplatesAsync(entity, "admin", force: true);
                created += result.Created.Count;
                updated += result.Updated.Count;
                regeneratedEntities.Add(entity.EntityRoute ?? entity.EntityName ?? string.Empty);
            }

            logger.LogInformation("[TemplateResetAll] Regeneration done. Entities={Count}, Created={Created}, Updated={Updated}",
                regeneratedEntities.Count, created, updated);

            return Results.Ok(new
            {
                entities = regeneratedEntities,
                deletedTemplates = allTemplates.Count,
                deletedStateBindings = stateBindings.Count,
                deletedLegacyBindings = legacyBindings.Count,
                created,
                updated
            });
        })
        .WithName("ResetAllTemplates")
        .WithSummary("Reset all templates (dev only)")
        .WithDescription("Delete all templates/bindings and regenerate system defaults for every entity.");

        return app;
    }

    private static string ResolveDocLanguage(IEndpointRouteBuilder app)
    {
        using var scope = app.ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var systemLang = db.SystemSettings
            .AsNoTracking()
            .Select(s => s.DefaultLanguage)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(systemLang))
        {
            return systemLang.ToLowerInvariant();
        }

        var fallback = db.LocalizationLanguages
            .AsNoTracking()
            .OrderBy(l => l.Id)
            .Select(l => l.Code)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(fallback) ? "en" : fallback.ToLowerInvariant();
    }

    private static string ResolveDocString(IEndpointRouteBuilder app, string lang, string key)
    {
        using var scope = app.ServiceProvider.CreateScope();
        var localization = scope.ServiceProvider.GetRequiredService<ILocalization>();
        return localization.T(key, lang);
    }
}
