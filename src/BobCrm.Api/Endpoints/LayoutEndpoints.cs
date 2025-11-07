using System.Security.Claims;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Core.DomainCommon;
using BobCrm.Api.Application.Queries;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Domain;
using BobCrm.Api.Contracts.DTOs;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// 布局管理相关端点
/// </summary>
public static class LayoutEndpoints
{
    public static IEndpointRouteBuilder MapLayoutEndpoints(this IEndpointRouteBuilder app)
    {
        var fieldsGroup = app.MapGroup("/api/fields")
            .WithTags("字段定义")
            .WithOpenApi()
            .RequireAuthorization();

        var layoutGroup = app.MapGroup("/api/layout")
            .WithTags("布局管理")
            .WithOpenApi()
            .RequireAuthorization();

        // 获取字段定义列表
        fieldsGroup.MapGet("", (IFieldQueries q, ILogger<Program> logger) =>
        {
            logger.LogDebug("[Fields] Retrieving field definitions");
            return Results.Json(q.GetDefinitions());
        })
        .WithName("GetFieldDefinitions")
        .WithSummary("获取字段定义列表")
        .WithDescription("获取所有自定义字段的定义信息");

        // 获取标签概览（用于快速布局）
        fieldsGroup.MapGet("/tags", (IRepository<FieldDefinition> repoDef, ILogger<Program> logger) =>
        {
            var defs = repoDef.Query().ToList();
            var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in defs)
            {
                if (string.IsNullOrWhiteSpace(d.Tags)) continue;
                try
                {
                    var tags = System.Text.Json.JsonSerializer.Deserialize<string[]>(d.Tags!) ?? Array.Empty<string>();
                    foreach (var t in tags)
                    {
                        if (string.IsNullOrWhiteSpace(t)) continue;
                        var k = t.Trim();
                        if (!dict.ContainsKey(k)) dict[k] = 0;
                        dict[k]++;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "[Fields] Failed to parse tags for field definition");
                }
            }
            var list = dict.OrderBy(kv => kv.Key).Select(kv => new { tag = kv.Key, count = kv.Value }).ToList();
            
            logger.LogDebug("[Fields] Retrieved {Count} unique tags", list.Count);
            return Results.Json(list);
        })
        .WithName("GetFieldTags")
        .WithSummary("获取字段标签统计")
        .WithDescription("获取所有字段定义中使用的标签及其计数");

        // 获取客户布局（已废弃，请使用 GET /api/layout）
        layoutGroup.MapGet("/{customerId:int}", (
            int customerId,
            ClaimsPrincipal user,
            ILayoutQueries q,
            ILogger<Program> logger,
            string? scope) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            scope ??= "effective";
            
            logger.LogWarning("[Layout] DEPRECATED: GET /api/layout/{{customerId}} is deprecated. Use GET /api/layout instead");
            logger.LogDebug("[Layout] Retrieving layout for customer {CustomerId}, user {UserId}, scope {Scope}", 
                customerId, uid, scope);
            // 忽略 customerId，统一查询 CustomerId=0
            return Results.Json(q.GetLayout(uid, 0, scope));
        })
        .WithName("GetCustomerLayout_Deprecated")
        .WithSummary("[已废弃] 获取客户布局")
        .WithDescription("[已废弃] 请使用 GET /api/layout 替代");

        // 保存客户布局（已废弃，请使用 POST /api/layout）
        layoutGroup.MapPost("/{customerId:int}", async (
            int customerId,
            ClaimsPrincipal user,
            IRepository<UserLayout> repoLayout,
            IUnitOfWork uow,
            System.Text.Json.JsonElement layout,
            HttpContext http,
            ILocalization loc,
            ILogger<Program> logger,
            string? scope) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var saveScope = (scope ?? "user").ToLowerInvariant();
            var targetUserId = saveScope == "default" ? "__default__" : uid;

            logger.LogWarning("[Layout] DEPRECATED: POST /api/layout/{{customerId}} is deprecated. Use POST /api/layout instead");
            logger.LogInformation("[Layout] Saving layout for customer {CustomerId} (redirecting to CustomerId=0), scope {Scope}, user {UserId}", 
                customerId, saveScope, uid);

            if (saveScope == "default")
            {
                var name = user.Identity?.Name ?? string.Empty;
                var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
                if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("[Layout] Access denied: non-admin user {UserId} attempted to save default layout", uid);
                    return Results.StatusCode(403);
                }
            }

            // 忽略 customerId，统一保存到 CustomerId=0
            var entity = repoLayout.Query(x => x.UserId == targetUserId && x.CustomerId == 0).FirstOrDefault();
            var json = layout.GetRawText();
            if (string.IsNullOrWhiteSpace(json))
            {
                var lang = LangHelper.GetLang(http);
                logger.LogWarning("[Layout] Validation failed: layout body is required");
                return ApiErrors.Validation(loc.T("ERR_LAYOUT_BODY_REQUIRED", lang));
            }

            if (entity == null)
            {
                entity = new UserLayout { UserId = targetUserId, CustomerId = 0, LayoutJson = json };
                await repoLayout.AddAsync(entity);
                logger.LogInformation("[Layout] Created new layout entry");
            }
            else
            {
                entity.LayoutJson = json;
                repoLayout.Update(entity);
                logger.LogInformation("[Layout] Updated existing layout entry");
            }
            await uow.SaveChangesAsync();
            return Results.Ok(ApiResponseExtensions.SuccessResponse("布局已保存"));
        })
        .WithName("SaveCustomerLayout_Deprecated")
        .WithSummary("[已废弃] 保存客户布局")
        .WithDescription("[已废弃] 请使用 POST /api/layout 替代");

        // 删除客户布局（已废弃，请使用 DELETE /api/layout）
        layoutGroup.MapDelete("/{customerId:int}", async (
            int customerId,
            ClaimsPrincipal user,
            IRepository<UserLayout> repoLayout,
            IUnitOfWork uow,
            ILogger<Program> logger,
            string? scope) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var delScope = (scope ?? "user").ToLowerInvariant();
            var targetUserId = delScope == "default" ? "__default__" : uid;

            logger.LogWarning("[Layout] DEPRECATED: DELETE /api/layout/{{customerId}} is deprecated. Use DELETE /api/layout instead");
            logger.LogInformation("[Layout] Deleting layout for customer {CustomerId} (redirecting to CustomerId=0), scope {Scope}, user {UserId}", 
                customerId, delScope, uid);

            if (delScope == "default")
            {
                var name = user.Identity?.Name ?? string.Empty;
                var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
                if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("[Layout] Access denied: non-admin user {UserId} attempted to delete default layout", uid);
                    return Results.StatusCode(403);
                }
            }

            // 忽略 customerId，统一删除 CustomerId=0
            var entity = repoLayout.Query(x => x.UserId == targetUserId && x.CustomerId == 0).FirstOrDefault();
            if (entity != null)
            {
                repoLayout.Remove(entity);
                await uow.SaveChangesAsync();
                logger.LogInformation("[Layout] Layout deleted successfully");
            }
            else
            {
                logger.LogDebug("[Layout] No layout found to delete");
            }
            
            return Results.Ok(ApiResponseExtensions.SuccessResponse("布局已删除"));
        })
        .WithName("DeleteCustomerLayout_Deprecated")
        .WithSummary("[已废弃] 删除客户布局")
        .WithDescription("[已废弃] 请使用 DELETE /api/layout 替代");

        // 获取用户级别布局（主端点）
        layoutGroup.MapGet("", (
            ClaimsPrincipal user,
            ILayoutQueries q,
            ILogger<Program> logger,
            string? scope) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            scope ??= "effective";
            
            logger.LogDebug("[Layout] Retrieving user-level layout, user {UserId}, scope {Scope}", uid, scope);
            // 使用 customerId = 0 作为用户级别布局的占位符
            return Results.Json(q.GetLayout(uid, 0, scope));
        })
        .WithName("GetLayout")
        .WithSummary("获取布局")
        .WithDescription("获取用户级别的布局模板，不绑定特定客户");

        // 获取用户级别布局（兼容别名，指向主端点）
        layoutGroup.MapGet("/customer", (
            ClaimsPrincipal user,
            ILayoutQueries q,
            ILogger<Program> logger,
            string? scope) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            scope ??= "effective";
            
            logger.LogDebug("[Layout] Retrieving user-level layout via /customer alias, user {UserId}, scope {Scope}", uid, scope);
            return Results.Json(q.GetLayout(uid, 0, scope));
        })
        .WithName("GetLayout_CustomerAlias")
        .WithSummary("获取布局（兼容别名）")
        .WithDescription("获取用户级别的布局模板（兼容旧路径 /api/layout/customer）");

        // 保存用户级别布局（主端点）
        layoutGroup.MapPost("", async (
            ClaimsPrincipal user,
            IRepository<UserLayout> repoLayout,
            IUnitOfWork uow,
            System.Text.Json.JsonElement layout,
            HttpContext http,
            ILocalization loc,
            ILogger<Program> logger,
            string? scope) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var saveScope = (scope ?? "user").ToLowerInvariant();
            var targetUserId = saveScope == "default" ? "__default__" : uid;

            logger.LogInformation("[Layout] Saving user-level layout, scope {Scope}, user {UserId}", saveScope, uid);

            if (saveScope == "default")
            {
                var name = user.Identity?.Name ?? string.Empty;
                var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
                if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("[Layout] Access denied: non-admin user {UserId} attempted to save default user-level layout", uid);
                    return Results.StatusCode(403);
                }
            }

            var body = System.Text.Json.JsonSerializer.Serialize(layout);
            var entity = repoLayout.Query(x => x.UserId == targetUserId && x.CustomerId == 0).FirstOrDefault();

            if (entity != null)
            {
                entity.LayoutJson = body;
                repoLayout.Update(entity);
                logger.LogInformation("[Layout] Updated existing user-level layout");
            }
            else
            {
                entity = new UserLayout
                {
                    UserId = targetUserId,
                    CustomerId = 0,  // 0 表示用户级别模板
                    LayoutJson = body
                };
                await repoLayout.AddAsync(entity);
                logger.LogInformation("[Layout] Created new user-level layout");
            }

            await uow.SaveChangesAsync();
            return Results.Ok(ApiResponseExtensions.SuccessResponse("布局已保存"));
        })
        .WithName("SaveLayout")
        .WithSummary("保存布局")
        .WithDescription("保存用户级别的布局模板");

        // 保存用户级别布局（兼容别名）
        layoutGroup.MapPost("/customer", async (
            ClaimsPrincipal user,
            IRepository<UserLayout> repoLayout,
            IUnitOfWork uow,
            System.Text.Json.JsonElement layout,
            HttpContext http,
            ILocalization loc,
            ILogger<Program> logger,
            string? scope) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var saveScope = (scope ?? "user").ToLowerInvariant();
            var targetUserId = saveScope == "default" ? "__default__" : uid;

            logger.LogInformation("[Layout] Saving user-level layout via /customer alias, scope {Scope}, user {UserId}", saveScope, uid);

            if (saveScope == "default")
            {
                var name = user.Identity?.Name ?? string.Empty;
                var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
                if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("[Layout] Access denied: non-admin user {UserId} attempted to save default user-level layout", uid);
                    return Results.StatusCode(403);
                }
            }

            var body = System.Text.Json.JsonSerializer.Serialize(layout);
            var entity = repoLayout.Query(x => x.UserId == targetUserId && x.CustomerId == 0).FirstOrDefault();

            if (entity != null)
            {
                entity.LayoutJson = body;
                repoLayout.Update(entity);
                logger.LogInformation("[Layout] Updated existing user-level layout");
            }
            else
            {
                entity = new UserLayout
                {
                    UserId = targetUserId,
                    CustomerId = 0,
                    LayoutJson = body
                };
                await repoLayout.AddAsync(entity);
                logger.LogInformation("[Layout] Created new user-level layout");
            }

            await uow.SaveChangesAsync();
            return Results.Ok(ApiResponseExtensions.SuccessResponse("布局已保存"));
        })
        .WithName("SaveLayout_CustomerAlias")
        .WithSummary("保存布局（兼容别名）")
        .WithDescription("保存用户级别的布局模板（兼容旧路径 /api/layout/customer）");

        // 删除用户级别布局（主端点）
        layoutGroup.MapDelete("", async (
            ClaimsPrincipal user,
            IRepository<UserLayout> repoLayout,
            IUnitOfWork uow,
            ILogger<Program> logger,
            string? scope) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var delScope = (scope ?? "user").ToLowerInvariant();
            var targetUserId = delScope == "default" ? "__default__" : uid;

            logger.LogInformation("[Layout] Deleting user-level layout, scope {Scope}, user {UserId}", delScope, uid);

            if (delScope == "default")
            {
                var name = user.Identity?.Name ?? string.Empty;
                var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
                if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("[Layout] Access denied: non-admin user {UserId} attempted to delete default user-level layout", uid);
                    return Results.StatusCode(403);
                }
            }

            var entity = repoLayout.Query(x => x.UserId == targetUserId && x.CustomerId == 0).FirstOrDefault();
            if (entity != null)
            {
                repoLayout.Remove(entity);
                await uow.SaveChangesAsync();
                logger.LogInformation("[Layout] User-level layout deleted successfully");
            }
            else
            {
                logger.LogDebug("[Layout] No user-level layout found to delete");
            }

            return Results.Ok(ApiResponseExtensions.SuccessResponse("布局已删除"));
        })
        .WithName("DeleteLayout")
        .WithSummary("删除布局")
        .WithDescription("删除用户级别的布局模板");

        // 删除用户级别布局（兼容别名）
        layoutGroup.MapDelete("/customer", async (
            ClaimsPrincipal user,
            IRepository<UserLayout> repoLayout,
            IUnitOfWork uow,
            ILogger<Program> logger,
            string? scope) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var delScope = (scope ?? "user").ToLowerInvariant();
            var targetUserId = delScope == "default" ? "__default__" : uid;

            logger.LogInformation("[Layout] Deleting user-level layout via /customer alias, scope {Scope}, user {UserId}", delScope, uid);

            if (delScope == "default")
            {
                var name = user.Identity?.Name ?? string.Empty;
                var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
                if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("[Layout] Access denied: non-admin user {UserId} attempted to delete default user-level layout", uid);
                    return Results.StatusCode(403);
                }
            }

            var entity = repoLayout.Query(x => x.UserId == targetUserId && x.CustomerId == 0).FirstOrDefault();
            if (entity != null)
            {
                repoLayout.Remove(entity);
                await uow.SaveChangesAsync();
                logger.LogInformation("[Layout] User-level layout deleted successfully");
            }
            else
            {
                logger.LogDebug("[Layout] No user-level layout found to delete");
            }

            return Results.Ok(ApiResponseExtensions.SuccessResponse("布局已删除"));
        })
        .WithName("DeleteLayout_CustomerAlias")
        .WithSummary("删除布局（兼容别名）")
        .WithDescription("删除用户级别的布局模板（兼容旧路径 /api/layout/customer）");

        // ===== 新版API：支持EntityType =====

        // 获取实体布局（根据EntityType）
        layoutGroup.MapGet("/entity/{entityType}", (
            string entityType,
            ClaimsPrincipal user,
            ILayoutQueries q,
            ILogger<Program> logger,
            string? scope) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            scope ??= "effective";

            logger.LogDebug("[Layout] Retrieving layout for entity {EntityType}, user {UserId}, scope {Scope}",
                entityType, uid, scope);

            return Results.Json(q.GetLayoutByEntityType(uid, entityType, scope));
        })
        .WithName("GetLayoutByEntityType")
        .WithSummary("获取实体布局（根据实体类型）")
        .WithDescription("根据实体类型（如customer、product、order）获取布局模板");

        // 保存实体布局（根据EntityType）
        layoutGroup.MapPost("/entity/{entityType}", async (
            string entityType,
            ClaimsPrincipal user,
            IRepository<UserLayout> repoLayout,
            IUnitOfWork uow,
            System.Text.Json.JsonElement layout,
            HttpContext http,
            ILocalization loc,
            ILogger<Program> logger,
            string? scope) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var saveScope = (scope ?? "user").ToLowerInvariant();
            var targetUserId = saveScope == "default" ? "__default__" : uid;

            logger.LogInformation("[Layout] Saving layout for entity {EntityType}, scope {Scope}, user {UserId}",
                entityType, saveScope, uid);

            if (saveScope == "default")
            {
                var name = user.Identity?.Name ?? string.Empty;
                var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
                if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("[Layout] Access denied: non-admin user {UserId} attempted to save default layout", uid);
                    return Results.StatusCode(403);
                }
            }

            var body = System.Text.Json.JsonSerializer.Serialize(layout);
            var entity = repoLayout.Query(x => x.UserId == targetUserId && x.EntityType == entityType).FirstOrDefault();

            if (entity != null)
            {
                entity.LayoutJson = body;
                repoLayout.Update(entity);
                logger.LogInformation("[Layout] Updated existing entity layout");
            }
            else
            {
                entity = new UserLayout
                {
                    UserId = targetUserId,
                    CustomerId = 0,  // 使用EntityType时，CustomerId设为0
                    EntityType = entityType,
                    LayoutJson = body
                };
                await repoLayout.AddAsync(entity);
                logger.LogInformation("[Layout] Created new entity layout");
            }

            await uow.SaveChangesAsync();
            return Results.Ok(ApiResponseExtensions.SuccessResponse("布局已保存"));
        })
        .WithName("SaveLayoutByEntityType")
        .WithSummary("保存实体布局（根据实体类型）")
        .WithDescription("根据实体类型保存布局模板");

        // 删除实体布局（根据EntityType）
        layoutGroup.MapDelete("/entity/{entityType}", async (
            string entityType,
            ClaimsPrincipal user,
            IRepository<UserLayout> repoLayout,
            IUnitOfWork uow,
            ILogger<Program> logger,
            string? scope) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var delScope = (scope ?? "user").ToLowerInvariant();
            var targetUserId = delScope == "default" ? "__default__" : uid;

            logger.LogInformation("[Layout] Deleting layout for entity {EntityType}, scope {Scope}, user {UserId}",
                entityType, delScope, uid);

            if (delScope == "default")
            {
                var name = user.Identity?.Name ?? string.Empty;
                var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
                if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("[Layout] Access denied: non-admin user {UserId} attempted to delete default layout", uid);
                    return Results.StatusCode(403);
                }
            }

            var entity = repoLayout.Query(x => x.UserId == targetUserId && x.EntityType == entityType).FirstOrDefault();
            if (entity != null)
            {
                repoLayout.Remove(entity);
                await uow.SaveChangesAsync();
                logger.LogInformation("[Layout] Entity layout deleted successfully");
            }
            else
            {
                logger.LogDebug("[Layout] No entity layout found to delete");
            }

            return Results.Ok(ApiResponseExtensions.SuccessResponse("布局已删除"));
        })
        .WithName("DeleteLayoutByEntityType")
        .WithSummary("删除实体布局（根据实体类型）")
        .WithDescription("根据实体类型删除布局模板");

        // 从标签生成布局
        layoutGroup.MapPost("/{customerId:int}/generate", async (
            int customerId,
            ClaimsPrincipal user,
            IRepository<FieldDefinition> repoDef,
            IRepository<UserLayout> repoLayout,
            IUnitOfWork uow,
            GenerateLayoutRequest req,
            ILocalization loc,
            HttpContext http,
            ILogger<Program> logger) =>
        {
            logger.LogInformation("[Layout] Generating layout for customer {CustomerId} from tags: {Tags}", 
                customerId, string.Join(", ", req.Tags ?? Array.Empty<string>()));

            if (req.Tags == null || req.Tags.Length == 0)
            {
                var lang = LangHelper.GetLang(http);
                logger.LogWarning("[Layout] Validation failed: tags are required");
                return ApiErrors.Validation(loc.T("ERR_TAGS_REQUIRED", lang));
            }

            var mode = string.Equals(req.Mode, "free", StringComparison.OrdinalIgnoreCase) ? "free" : "flow";
            var defs = repoDef.Query().ToList();
            var tagSet = new HashSet<string>(req.Tags.Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)), StringComparer.OrdinalIgnoreCase);

            bool HasTag(FieldDefinition d)
            {
                if (string.IsNullOrWhiteSpace(d.Tags)) return false;
                try
                {
                    var tags = System.Text.Json.JsonSerializer.Deserialize<string[]>(d.Tags!) ?? Array.Empty<string>();
                    return tags.Any(t => tagSet.Contains(t));
                }
                catch { return false; }
            }

            var withTag = defs.Where(HasTag).ToList();
            var others = defs.Except(withTag).ToList();
            var ordered = withTag.Concat(others).ToList();

            var items = new Dictionary<string, object?>();
            if (mode == "flow")
            {
                for (int i = 0; i < ordered.Count; i++)
                {
                    var d = ordered[i];
                    items[d.Key] = new { order = i, w = 6 };
                }
            }
            else // free
            {
                var columns = 12; var w = 3; var h = 1; var perRow = Math.Max(1, columns / w);
                for (int i = 0; i < ordered.Count; i++)
                {
                    var d = ordered[i];
                    var col = i % perRow; var row = i / perRow;
                    items[d.Key] = new { x = col * w, y = row * h, w, h };
                }
            }

            var jsonObj = new { mode, items };

            if (req.Save == true)
            {
                var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
                var scope = (req.Scope ?? "user").ToLowerInvariant();
                var targetUserId = scope == "default" ? "__default__" : uid;
                if (scope == "default")
                {
                    var name = user.Identity?.Name ?? string.Empty;
                    var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
                    if (!string.Equals(name, "admin", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogWarning("[Layout] Access denied: non-admin user {UserId} attempted to save generated layout as default", uid);
                        return Results.StatusCode(403);
                    }
                }

                var json = System.Text.Json.JsonSerializer.Serialize(jsonObj);
                var entity = repoLayout.Query(x => x.UserId == targetUserId && x.CustomerId == customerId).FirstOrDefault();
                if (entity == null)
                {
                    entity = new UserLayout { UserId = targetUserId, CustomerId = customerId, LayoutJson = json };
                    await repoLayout.AddAsync(entity);
                    logger.LogInformation("[Layout] Generated and saved new layout");
                }
                else
                {
                    entity.LayoutJson = json;
                    repoLayout.Update(entity);
                    logger.LogInformation("[Layout] Generated and updated existing layout");
                }
                await uow.SaveChangesAsync();
            }
            else
            {
                logger.LogInformation("[Layout] Generated layout (not saved)");
            }

            return Results.Json(jsonObj);
        })
        .WithName("GenerateLayout")
        .WithSummary("从标签生成布局")
        .WithDescription("根据指定的标签自动生成布局配置，可选择是否保存");

        return app;
    }
}

public record GenerateLayoutRequest(string[] Tags, string? Mode, bool? Save, string? Scope);
