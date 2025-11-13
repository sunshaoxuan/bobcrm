using System.Security.Claims;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Core.DomainCommon;
using BobCrm.Api.Domain;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Services;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// 表单模板管理端点
/// </summary>
public static class TemplateEndpoints
{
    public static IEndpointRouteBuilder MapTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/templates")
            .WithTags("表单模板")
            .WithOpenApi()
            .RequireAuthorization();

        // 获取用户的所有模板
        group.MapGet("", async (
            ClaimsPrincipal user,
            IRepository<FormTemplate> repo,
            ILogger<Program> logger,
            string? entityType,
            string? groupBy) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            logger.LogDebug("[Templates] Retrieving templates for user {UserId}, entityType: {EntityType}, groupBy: {GroupBy}",
                uid, entityType ?? "all", groupBy ?? "none");

            var query = repo.Query(t => t.UserId == uid);

            // 按实体类型过滤
            if (!string.IsNullOrWhiteSpace(entityType))
            {
                query = query.Where(t => t.EntityType == entityType);
            }

            var templates = await Task.FromResult(query.OrderByDescending(t => t.IsUserDefault)
                .ThenByDescending(t => t.UpdatedAt)
                .ToList());

            // 按分组方式组织数据
            if (groupBy == "entity")
            {
                var grouped = templates.GroupBy(t => t.EntityType ?? "未分类")
                    .Select(g => new
                    {
                        EntityType = g.Key,
                        Templates = g.Select(t => new
                        {
                            t.Id,
                            t.Name,
                            t.EntityType,
                            t.IsUserDefault,
                            t.IsSystemDefault,
                            t.Description,
                            t.CreatedAt,
                            t.UpdatedAt,
                            t.IsInUse
                        })
                    });
                return Results.Json(grouped);
            }
            else if (groupBy == "user")
            {
                // 按用户分组（未来扩展，管理员可以看到所有用户的模板）
                var grouped = new[]
                {
                    new
                    {
                        UserId = uid,
                        Templates = templates.Select(t => new
                        {
                            t.Id,
                            t.Name,
                            t.EntityType,
                            t.IsUserDefault,
                            t.IsSystemDefault,
                            t.Description,
                            t.CreatedAt,
                            t.UpdatedAt,
                            t.IsInUse
                        })
                    }
                };
                return Results.Json(grouped);
            }
            else
            {
                // 平铺列表
                var result = templates.Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.EntityType,
                    t.IsUserDefault,
                    t.IsSystemDefault,
                    t.Description,
                    t.CreatedAt,
                    t.UpdatedAt,
                    t.IsInUse
                });
                return Results.Json(result);
            }
        })
        .WithName("GetTemplates")
        .WithSummary("获取用户的表单模板列表")
        .WithDescription("支持按实体类型过滤，支持按实体或用户分组");

        // 获取单个模板详情
        group.MapGet("/{id:int}", async (
            int id,
            ClaimsPrincipal user,
            IRepository<FormTemplate> repo,
            ILogger<Program> logger) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var template = await Task.FromResult(repo.Query(t => t.Id == id && t.UserId == uid).FirstOrDefault());

            if (template == null)
            {
                logger.LogWarning("[Templates] Template {TemplateId} not found for user {UserId}", id, uid);
                return Results.NotFound(new { error = "模板不存在" });
            }

            return Results.Json(template);
        })
        .WithName("GetTemplate")
        .WithSummary("获取模板详情")
        .WithDescription("根据模板ID获取完整的模板信息");

        // 创建新模板
        group.MapPost("", async (
            ClaimsPrincipal user,
            IRepository<FormTemplate> repo,
            IUnitOfWork uow,
            CreateTemplateRequest req,
            ILogger<Program> logger) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            logger.LogInformation("[Templates] Creating template for user {UserId}, name: {Name}, entityType: {EntityType}",
                uid, req.Name, req.EntityType ?? "null");

            // 如果设置为用户默认模板，需要取消同一实体类型下的其他用户默认模板
            if (req.IsUserDefault && !string.IsNullOrWhiteSpace(req.EntityType))
            {
                var existingDefaults = repo.Query(t => t.UserId == uid &&
                    t.EntityType == req.EntityType &&
                    t.IsUserDefault).ToList();

                foreach (var existing in existingDefaults)
                {
                    existing.IsUserDefault = false;
                    repo.Update(existing);
                }

                logger.LogInformation("[Templates] Cleared {Count} existing user default templates", existingDefaults.Count);
            }

            var template = new FormTemplate
            {
                Name = req.Name,
                EntityType = req.EntityType,
                UserId = uid,
                IsUserDefault = req.IsUserDefault,
                IsSystemDefault = false, // 只有管理员可以设置系统默认
                LayoutJson = req.LayoutJson,
                Description = req.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsInUse = false
            };

            await repo.AddAsync(template);
            await uow.SaveChangesAsync();

            logger.LogInformation("[Templates] Template created successfully with ID {TemplateId}", template.Id);

            return Results.Created($"/api/templates/{template.Id}", template);
        })
        .WithName("CreateTemplate")
        .WithSummary("创建新模板")
        .WithDescription("创建一个新的表单模板");

        // 更新模板
        group.MapPut("/{id:int}", async (
            int id,
            ClaimsPrincipal user,
            IRepository<FormTemplate> repo,
            IUnitOfWork uow,
            UpdateTemplateRequest req,
            ILogger<Program> logger) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var template = await Task.FromResult(repo.Query(t => t.Id == id && t.UserId == uid).FirstOrDefault());

            if (template == null)
            {
                logger.LogWarning("[Templates] Template {TemplateId} not found for user {UserId}", id, uid);
                return Results.NotFound(new { error = "模板不存在" });
            }

            logger.LogInformation("[Templates] Updating template {TemplateId} for user {UserId}", id, uid);

            // EntityType一旦设置后不允许修改
            if (!string.IsNullOrWhiteSpace(template.EntityType) &&
                req.EntityType != null &&
                req.EntityType != template.EntityType)
            {
                logger.LogWarning("[Templates] Attempted to change EntityType from {Old} to {New}",
                    template.EntityType, req.EntityType);
                return Results.BadRequest(new { error = "实体类型一旦设置后不允许修改" });
            }

            // 如果设置为用户默认模板，需要取消同一实体类型下的其他用户默认模板
            if (req.IsUserDefault == true && !string.IsNullOrWhiteSpace(template.EntityType))
            {
                var existingDefaults = repo.Query(t => t.UserId == uid &&
                    t.EntityType == template.EntityType &&
                    t.IsUserDefault &&
                    t.Id != id).ToList();

                foreach (var existing in existingDefaults)
                {
                    existing.IsUserDefault = false;
                    repo.Update(existing);
                }
            }

            // 更新字段
            if (req.Name != null) template.Name = req.Name;
            if (req.EntityType != null && string.IsNullOrWhiteSpace(template.EntityType))
            {
                template.EntityType = req.EntityType;
            }
            if (req.IsUserDefault != null) template.IsUserDefault = req.IsUserDefault.Value;
            if (req.LayoutJson != null) template.LayoutJson = req.LayoutJson;
            if (req.Description != null) template.Description = req.Description;

            template.UpdatedAt = DateTime.UtcNow;

            repo.Update(template);
            await uow.SaveChangesAsync();

            logger.LogInformation("[Templates] Template {TemplateId} updated successfully", id);

            return Results.Ok(template);
        })
        .WithName("UpdateTemplate")
        .WithSummary("更新模板")
        .WithDescription("更新模板信息（EntityType一旦设置后不允许修改）");

        // 删除模板
        group.MapDelete("/{id:int}", async (
            int id,
            ClaimsPrincipal user,
            IRepository<FormTemplate> repo,
            IUnitOfWork uow,
            ILogger<Program> logger) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var template = await Task.FromResult(repo.Query(t => t.Id == id && t.UserId == uid).FirstOrDefault());

            if (template == null)
            {
                logger.LogWarning("[Templates] Template {TemplateId} not found for user {UserId}", id, uid);
                return Results.NotFound(new { error = "模板不存在" });
            }

            // 系统默认模板不允许删除
            if (template.IsSystemDefault)
            {
                logger.LogWarning("[Templates] Attempted to delete system default template {TemplateId}", id);
                return Results.BadRequest(new { error = "系统默认模板不允许删除" });
            }

            // 用户默认模板不允许删除
            if (template.IsUserDefault)
            {
                logger.LogWarning("[Templates] Attempted to delete user default template {TemplateId}", id);
                return Results.BadRequest(new { error = "用户默认模板不允许删除，请先设置其他模板为默认模板" });
            }

            // 正在使用的模板不允许删除
            if (template.IsInUse)
            {
                logger.LogWarning("[Templates] Attempted to delete in-use template {TemplateId}", id);
                return Results.BadRequest(new { error = "正在使用的模板不允许删除" });
            }

            repo.Remove(template);
            await uow.SaveChangesAsync();

            logger.LogInformation("[Templates] Template {TemplateId} deleted successfully", id);

            return Results.Ok(ApiResponseExtensions.SuccessResponse("模板已删除"));
        })
        .WithName("DeleteTemplate")
        .WithSummary("删除模板")
        .WithDescription("删除模板（系统默认、用户默认和正在使用的模板不允许删除）");

        // 获取有效模板（用于PageLoader）
        group.MapGet("/effective/{entityType}", async (
            string entityType,
            ClaimsPrincipal user,
            IRepository<FormTemplate> repo,
            ILogger<Program> logger) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            logger.LogDebug("[Templates] Getting effective template for entity {EntityType}, user {UserId}",
                entityType, uid);

            // 1. 优先查找用户默认模板
            var userDefault = await Task.FromResult(repo.Query(t =>
                t.UserId == uid &&
                t.EntityType == entityType &&
                t.IsUserDefault).FirstOrDefault());

            if (userDefault != null)
            {
                logger.LogDebug("[Templates] Found user default template {TemplateId}", userDefault.Id);
                return Results.Json(userDefault);
            }

            // 2. 查找系统默认模板
            var systemDefault = await Task.FromResult(repo.Query(t =>
                t.EntityType == entityType &&
                t.IsSystemDefault).FirstOrDefault());

            if (systemDefault != null)
            {
                logger.LogDebug("[Templates] Found system default template {TemplateId}", systemDefault.Id);
                return Results.Json(systemDefault);
            }

            // 3. 查找该实体类型的第一个模板（任意用户）
            var firstTemplate = await Task.FromResult(repo.Query(t => t.EntityType == entityType)
                .OrderBy(t => t.CreatedAt)
                .FirstOrDefault());

            if (firstTemplate != null)
            {
                logger.LogDebug("[Templates] Found first template {TemplateId} for entity type", firstTemplate.Id);
                return Results.Json(firstTemplate);
            }

            logger.LogDebug("[Templates] No template found for entity {EntityType}", entityType);
            return Results.NotFound(new { error = "未找到模板" });
        })
        .WithName("GetEffectiveTemplate")
        .WithSummary("获取有效模板")
        .WithDescription("按优先级获取模板：用户默认 > 系统默认 > 第一个创建的模板");

        group.MapGet("/bindings/{entityType}", async (
            string entityType,
            FormTemplateUsageType? usageType,
            TemplateBindingService bindingService,
            CancellationToken ct) =>
        {
            var resolvedUsage = usageType ?? FormTemplateUsageType.Detail;
            var binding = await bindingService.GetBindingAsync(entityType, resolvedUsage, ct);
            return binding is null
                ? Results.NotFound(new { error = "Template binding not found." })
                : Results.Ok(binding.ToDto());
        })
        .WithName("GetTemplateBinding")
        .WithSummary("获取实体模板绑定")
        .WithDescription("按照实体与用途获取模板绑定记录。");

        group.MapPut("/bindings", async (
            UpsertTemplateBindingRequest request,
            ClaimsPrincipal user,
            TemplateBindingService bindingService,
            CancellationToken ct) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid))
            {
                return Results.Unauthorized();
            }

            var binding = await bindingService.UpsertBindingAsync(
                request.EntityType,
                request.UsageType,
                request.TemplateId,
                request.IsSystem,
                uid,
                request.RequiredFunctionCode,
                ct);

            return Results.Ok(binding.ToDto());
        })
        .WithName("UpsertTemplateBinding")
        .WithSummary("更新模板绑定")
        .WithDescription("创建或更新实体与模板之间的绑定关系。");

        group.MapPost("/runtime/{entityType}", async (
            string entityType,
            TemplateRuntimeRequest request,
            ClaimsPrincipal user,
            TemplateRuntimeService runtimeService,
            CancellationToken ct) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid))
            {
                return Results.Unauthorized();
            }

            request ??= new TemplateRuntimeRequest();
            var context = await runtimeService.BuildRuntimeContextAsync(uid, entityType, request, ct);
            return Results.Ok(context);
        })
        .WithName("BuildTemplateRuntime")
        .WithSummary("获取模板运行时上下文")
        .WithDescription("结合权限与数据范围返回模板所需的运行时信息。");

        return app;
    }
}

// 请求DTO
public record CreateTemplateRequest(
    string Name,
    string? EntityType,
    bool IsUserDefault,
    string? LayoutJson,
    string? Description);

public record UpdateTemplateRequest(
    string? Name,
    string? EntityType,
    bool? IsUserDefault,
    string? LayoutJson,
    string? Description);
