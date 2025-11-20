using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Core.DomainCommon;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Services;
using Microsoft.EntityFrameworkCore;

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

        // 获取用户的所有模板（包括系统模板）
        group.MapGet("", async (
            ClaimsPrincipal user,
            IRepository<FormTemplate> repo,
            ILogger<Program> logger,
            string? entityType,
            string? usageType,
            string? templateType,
            string? groupBy) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            logger.LogDebug("[Templates] Retrieving templates for user {UserId}, entityType: {EntityType}, usageType: {UsageType}, templateType: {TemplateType}",
                uid, entityType ?? "all", usageType ?? "all", templateType ?? "all");

            // Query both user templates and system templates
            var query = repo.Query(t => t.UserId == uid || t.IsSystemDefault);

            // 按实体类型过滤
            if (!string.IsNullOrWhiteSpace(entityType))
            {
                query = query.Where(t => t.EntityType == entityType);
            }

            // 按用途过滤
            if (!string.IsNullOrWhiteSpace(usageType) && Enum.TryParse<FormTemplateUsageType>(usageType, true, out var parsedUsageType))
            {
                query = query.Where(t => t.UsageType == parsedUsageType);
            }

            // 按模板类型过滤（system/user）
            if (!string.IsNullOrWhiteSpace(templateType))
            {
                if (templateType.Equals("system", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(t => t.IsSystemDefault);
                }
                else if (templateType.Equals("user", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(t => t.UserId == uid && !t.IsSystemDefault);
                }
            }

            var templates = await Task.FromResult(query
                .OrderByDescending(t => t.IsSystemDefault)
                .ThenByDescending(t => t.IsUserDefault)
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

        // 复制模板
        group.MapPost("/{id:int}/copy", async (
            int id,
            ClaimsPrincipal user,
            IRepository<FormTemplate> repo,
            IUnitOfWork uow,
            CopyTemplateRequest req,
            ILogger<Program> logger) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            // 查找源模板（可以是系统模板或用户模板）
            var sourceTemplate = await Task.FromResult(
                repo.Query(t => t.Id == id && (t.UserId == uid || t.IsSystemDefault))
                    .FirstOrDefault());

            if (sourceTemplate == null)
            {
                logger.LogWarning("[Templates] Template {TemplateId} not found for copying by user {UserId}", id, uid);
                return Results.NotFound(new { error = "模板不存在" });
            }

            logger.LogInformation("[Templates] Copying template {TemplateId} for user {UserId}, new name: {Name}",
                id, uid, req.Name);

            // 创建新模板（深拷贝）
            var newTemplate = new FormTemplate
            {
                Name = req.Name ?? $"{sourceTemplate.Name} (Copy)",
                EntityType = req.EntityType ?? sourceTemplate.EntityType,
                UserId = uid,
                IsUserDefault = false, // 复制的模板默认不是用户默认模板
                IsSystemDefault = false, // 用户创建的模板不能是系统模板
                LayoutJson = sourceTemplate.LayoutJson,
                UsageType = req.UsageType ?? sourceTemplate.UsageType,
                Description = req.Description ?? $"从 '{sourceTemplate.Name}' 复制",
                Tags = sourceTemplate.Tags != null ? new List<string>(sourceTemplate.Tags) : null,
                RequiredFunctionCode = sourceTemplate.RequiredFunctionCode,
                LayoutMode = sourceTemplate.LayoutMode,
                DetailDisplayMode = sourceTemplate.DetailDisplayMode,
                DetailRoute = sourceTemplate.DetailRoute,
                ModalSize = sourceTemplate.ModalSize,
                Version = 1, // 新模板版本从 1 开始
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsInUse = false
            };

            await repo.AddAsync(newTemplate);
            await uow.SaveChangesAsync();

            logger.LogInformation("[Templates] Template copied successfully, new ID: {TemplateId}", newTemplate.Id);

            return Results.Created($"/api/templates/{newTemplate.Id}", newTemplate);
        })
        .WithName("CopyTemplate")
        .WithSummary("复制模板")
        .WithDescription("从现有模板创建副本（可以从系统模板或用户模板复制）");

        // 应用模板（设置为用户默认模板）
        group.MapPut("/{id:int}/apply", async (
            int id,
            ClaimsPrincipal user,
            IRepository<FormTemplate> repo,
            IUnitOfWork uow,
            ILogger<Program> logger) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            // 查找要应用的模板
            var template = await Task.FromResult(
                repo.Query(t => t.Id == id && (t.UserId == uid || t.IsSystemDefault))
                    .FirstOrDefault());

            if (template == null)
            {
                logger.LogWarning("[Templates] Template {TemplateId} not found for applying by user {UserId}", id, uid);
                return Results.NotFound(new { error = "模板不存在" });
            }

            if (string.IsNullOrWhiteSpace(template.EntityType))
            {
                logger.LogWarning("[Templates] Cannot apply template {TemplateId} without EntityType", id);
                return Results.BadRequest(new { error = "模板缺少实体类型，无法应用" });
            }

            logger.LogInformation("[Templates] Applying template {TemplateId} for user {UserId}, entity: {EntityType}",
                id, uid, template.EntityType);

            // 如果是系统模板，需要先复制一份给用户
            if (template.IsSystemDefault)
            {
                // 检查用户是否已经有这个模板的副本
                var existingCopy = await Task.FromResult(
                    repo.Query(t => t.UserId == uid &&
                                   t.EntityType == template.EntityType &&
                                   t.UsageType == template.UsageType &&
                                   t.Name == template.Name)
                        .FirstOrDefault());

                if (existingCopy != null)
                {
                    // 使用现有副本
                    template = existingCopy;
                }
                else
                {
                    // 创建新副本
                    var copy = new FormTemplate
                    {
                        Name = template.Name,
                        EntityType = template.EntityType,
                        UserId = uid,
                        IsUserDefault = false,
                        IsSystemDefault = false,
                        LayoutJson = template.LayoutJson,
                        UsageType = template.UsageType,
                        Description = $"从系统模板 '{template.Name}' 复制",
                        Tags = template.Tags != null ? new List<string>(template.Tags) : null,
                        RequiredFunctionCode = template.RequiredFunctionCode,
                        LayoutMode = template.LayoutMode,
                        DetailDisplayMode = template.DetailDisplayMode,
                        DetailRoute = template.DetailRoute,
                        ModalSize = template.ModalSize,
                        Version = 1,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsInUse = false
                    };

                    await repo.AddAsync(copy);
                    template = copy;
                }
            }

            // 取消同一实体类型和用途下的其他用户默认模板
            var existingDefaults = repo.Query(t => t.UserId == uid &&
                t.EntityType == template.EntityType &&
                t.UsageType == template.UsageType &&
                t.IsUserDefault &&
                t.Id != template.Id).ToList();

            foreach (var existing in existingDefaults)
            {
                existing.IsUserDefault = false;
                repo.Update(existing);
                logger.LogDebug("[Templates] Cleared user default flag from template {TemplateId}", existing.Id);
            }

            // 设置为用户默认模板
            template.IsUserDefault = true;
            template.UpdatedAt = DateTime.UtcNow;
            repo.Update(template);

            await uow.SaveChangesAsync();

            logger.LogInformation("[Templates] Template {TemplateId} applied successfully as user default", template.Id);

            return Results.Ok(new
            {
                message = "模板已应用为默认模板",
                template = new
                {
                    template.Id,
                    template.Name,
                    template.EntityType,
                    template.UsageType,
                    template.IsUserDefault,
                    template.IsSystemDefault
                }
            });
        })
        .WithName("ApplyTemplate")
        .WithSummary("应用模板")
        .WithDescription("将模板设置为用户默认模板（如果是系统模板，会先创建副本）");

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

        group.MapGet("/menu-bindings", async (
            ClaimsPrincipal user,
            AppDbContext db,
            ILogger<Program> logger,
            FormTemplateUsageType? usageType,
            CancellationToken ct) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(uid))
            {
                return Results.Unauthorized();
            }

            var resolvedUsage = usageType ?? FormTemplateUsageType.Detail;
            var now = DateTime.UtcNow;

            // Get system default language for display name resolution
            string systemLanguage;
            try
            {
                systemLanguage = await db.SystemSettings
                    .AsNoTracking()
                    .Select(s => s.DefaultLanguage)
                    .FirstOrDefaultAsync(ct) ?? "zh";
            }
            catch
            {
                // If SystemSettings table doesn't exist yet (during initialization), default to Chinese
                systemLanguage = "zh";
            }

            var accessibleFunctionIds = await db.RoleAssignments
                .Where(a => a.UserId == uid &&
                            (!a.ValidFrom.HasValue || a.ValidFrom <= now) &&
                            (!a.ValidTo.HasValue || a.ValidTo >= now))
                .SelectMany(a => a.Role!.Functions)
                .Select(rf => rf.FunctionId)
                .Distinct()
                .ToListAsync(ct);

            if (accessibleFunctionIds.Count == 0)
            {
                return Results.Ok(Array.Empty<object>());
            }

            var menuNodes = await db.FunctionNodes
                .AsNoTracking()
                .Include(fn => fn.TemplateBinding!)
                .ThenInclude(tb => tb.Template)
                .Where(fn => fn.TemplateBindingId != null && accessibleFunctionIds.Contains(fn.Id))
                .ToListAsync(ct);

            var filteredNodes = menuNodes
                .Where(fn => fn.TemplateBinding != null && fn.TemplateBinding.UsageType == resolvedUsage)
                .OrderBy(fn => fn.SortOrder)
                .ToList();

            if (filteredNodes.Count == 0)
            {
                return Results.Ok(Array.Empty<object>());
            }

            var entityTypes = filteredNodes
                .Select(fn => fn.TemplateBinding!.EntityType)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var entityTypeSet = new HashSet<string>(entityTypes, StringComparer.OrdinalIgnoreCase);

            var entityMetadata = await db.EntityDefinitions
                .AsNoTracking()
                .Where(ed => ed.EntityRoute != null && entityTypeSet.Contains(ed.EntityRoute))
                .ToDictionaryAsync(
                    ed => ed.EntityRoute!,
                    ed => new EntityMenuMetadata(
                        ResolveDisplayName(ed, systemLanguage),
                        ResolveRoute(ed)),
                    StringComparer.OrdinalIgnoreCase,
                    ct);

            var candidateTemplates = await db.FormTemplates
                .AsNoTracking()
                .Where(t => t.EntityType != null &&
                            entityTypeSet.Contains(t.EntityType!) &&
                            (t.UserId == uid || t.IsSystemDefault))
                .ToListAsync(ct);

            var templatesByEntity = candidateTemplates
                .GroupBy(t => t.EntityType!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var result = new List<object>(filteredNodes.Count);
            foreach (var node in filteredNodes)
            {
                var binding = node.TemplateBinding!;
                var key = binding.EntityType;
                templatesByEntity.TryGetValue(key, out var templateList);
                templateList ??= new List<FormTemplate>();

                if (binding.Template != null && templateList.All(t => t.Id != binding.TemplateId))
                {
                    templateList = new List<FormTemplate>(templateList) { binding.Template };
                }

                var templates = templateList
                    .OrderByDescending(t => t.IsUserDefault)
                    .ThenByDescending(t => t.IsSystemDefault)
                    .ThenBy(t => t.Name)
                    .Select(t => new
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
                    .ToList();

                entityMetadata.TryGetValue(binding.EntityType, out var metadata);
                var displayName = metadata?.DisplayName ?? node.Name;
                var resolvedRoute = metadata?.Route ?? node.Route;

                var menuPayload = new
                {
                    node.Id,
                    Code = NormalizeMenuCode(node.Code),
                    Name = displayName,
                    node.DisplayNameKey,
                    DisplayName = node.DisplayName == null
                        ? null
                        : new Dictionary<string, string?>(node.DisplayName, StringComparer.OrdinalIgnoreCase),
                    Route = resolvedRoute,
                    node.Icon,
                    node.SortOrder
                };

                var entry = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Menu"] = menuPayload,
                    ["Binding"] = binding.ToDto(),
                    ["Templates"] = templates
                };

                result.Add(entry);
            }

            logger.LogDebug("[Templates] Calculated {Count} menu/template intersections for user {UserId}.", result.Count, uid);
            return Results.Ok(result);
        })
        .WithName("GetMenuTemplateIntersections")
        .WithSummary("获取菜单与模板交集")
        .WithDescription("返回当前用户可访问的菜单节点及其模板绑定和可选模板列表。");

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

    private static string NormalizeMenuCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        if (code.EndsWith(".DETAIL", StringComparison.OrdinalIgnoreCase) ||
            code.EndsWith(".EDIT", StringComparison.OrdinalIgnoreCase))
        {
            var index = code.LastIndexOf('.');
            return index > 0 ? code[..index] : code;
        }

        return code;
    }

    private static string ResolveDisplayName(EntityDefinition definition, string preferredLanguage)
    {
        if (definition.DisplayName != null)
        {
            // Try preferred language first
            if (definition.DisplayName.TryGetValue(preferredLanguage, out var preferredValue) &&
                !string.IsNullOrWhiteSpace(preferredValue))
            {
                return preferredValue!;
            }

            // Fall back to first available translation
            var fallbackValue = definition.DisplayName.Values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
            if (!string.IsNullOrWhiteSpace(fallbackValue))
            {
                return fallbackValue!;
            }
        }

        return definition.EntityName;
    }

    private static string? ResolveRoute(EntityDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(definition.ApiEndpoint))
        {
            return null;
        }

        var route = definition.ApiEndpoint;
        if (route.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            route = route[4..];
        }
        return route;
    }

    private sealed record EntityMenuMetadata(string DisplayName, string? Route);
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

public record CopyTemplateRequest(
    string? Name,
    string? EntityType,
    FormTemplateUsageType? UsageType,
    string? Description);
