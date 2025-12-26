using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Core.DomainCommon;
using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.Requests.Entity;
using BobCrm.Api.Contracts.Responses.Entity;
using BobCrm.Api.Extensions;
using BobCrm.Api.Utils;
using Microsoft.AspNetCore.Http;
using BobCrm.Api.Services;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// 实体定义管理端点 - 支持实体自定义与发布
/// </summary>
public static class EntityDefinitionEndpoints
{
    public static IEndpointRouteBuilder MapEntityDefinitionEndpoints(this IEndpointRouteBuilder app)
    {
        // ==================== 实体元数据端点（公共访问）====================
        var entitiesGroup = app.MapGroup("/api/entities")
            .WithTags("实体元数据")
            .WithOpenApi();

        static List<string> BuildEntityCandidates(string entityType)
        {
            var normalized = entityType?.Trim().Trim('/').ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized)) return new List<string>();

            if (normalized.StartsWith("entity_"))
            {
                normalized = normalized["entity_".Length..];
            }

            var list = new List<string> { normalized };
            if (normalized.EndsWith("s"))
            {
                list.Add(normalized.TrimEnd('s'));
            }
            else
            {
                list.Add($"{normalized}s");
            }

            return list.Distinct().ToList();
        }

        // 获取可用实体列表（公共端点，不需要认证）
        entitiesGroup.MapGet("", async (string? lang, HttpContext http, AppDbContext db) =>
        {
            var targetLang = LangHelper.GetLang(http, lang);
            var entities = await db.EntityDefinitions
                .Where(ed => ed.IsEnabled && ed.Status == EntityStatus.Published)
                .OrderBy(ed => ed.Order)
                .AsNoTracking()
                .ToListAsync();

            var dtos = entities
                .Select(ed => ed.ToSummaryDto(targetLang))
                .ToList();

            return Results.Ok(new SuccessResponse<List<EntitySummaryDto>>(dtos));
        })
        .WithName("GetAvailableEntities")
        .WithSummary("获取可用实体列表")
        .WithDescription("获取所有已启用且已发布的实体元数据（公共访问）")
        .Produces<SuccessResponse<List<EntitySummaryDto>>>()
        .AllowAnonymous();

        entitiesGroup.MapGet("/{entityType}/definition", async (string entityType, string? lang, AppDbContext db, ILocalization loc, HttpContext http) =>
        {
            var uiLang = LangHelper.GetLang(http);
            var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
            var candidates = BuildEntityCandidates(entityType);

            var definition = await db.EntityDefinitions
                .AsNoTracking()
                .Include(ed => ed.Fields.OrderBy(f => f.SortOrder))
                .Include(ed => ed.Interfaces)
                .FirstOrDefaultAsync(ed =>
                    candidates.Contains(ed.EntityRoute.ToLower()) ||
                    candidates.Contains(ed.EntityName.ToLower()) ||
                    candidates.Contains(ed.FullTypeName.ToLower()));

            if (definition == null)
            {
                return Results.NotFound(new ErrorResponse(loc.T("ERR_ENTITY_NOT_FOUND", uiLang), "ENTITY_NOT_FOUND"));
            }

            var dto = new EntityDefinitionDto
            {
                Id = definition.Id,
                Namespace = definition.Namespace,
                EntityName = definition.EntityName,
                FullTypeName = definition.FullTypeName,
                EntityRoute = definition.EntityRoute,
                DisplayName = targetLang != null ? definition.DisplayName.Resolve(targetLang) : null,
                Description = targetLang != null ? definition.Description.Resolve(targetLang) : null,
                DisplayNameTranslations = targetLang == null
                    ? (definition.DisplayName != null ? new MultilingualText(definition.DisplayName) : new MultilingualText())
                    : null,
                DescriptionTranslations = targetLang == null
                    ? (definition.Description != null ? new MultilingualText(definition.Description) : null)
                    : null,
                ApiEndpoint = definition.ApiEndpoint,
                StructureType = definition.StructureType,
                Status = definition.Status,
                Source = definition.Source,
                IsLocked = definition.IsLocked,
                IsRootEntity = definition.IsRootEntity,
                IsEnabled = definition.IsEnabled,
                Order = definition.Order,
                Icon = definition.Icon,
                Category = definition.Category,
                CreatedAt = definition.CreatedAt,
                UpdatedAt = definition.UpdatedAt,
                CreatedBy = definition.CreatedBy,
                UpdatedBy = definition.UpdatedBy,
                Fields = definition.Fields
                    .Where(f => !f.IsDeleted)
                    .OrderBy(f => f.SortOrder)
                    .Select(f => f.ToFieldDto(loc, targetLang))
                    .ToList(),
                Interfaces = definition.Interfaces
                    .Where(i => i.IsEnabled)
                    .Select(i => new EntityInterfaceDto { Id = i.Id, InterfaceType = i.InterfaceType, IsEnabled = i.IsEnabled })
                    .ToList()
            };

            return Results.Ok(new SuccessResponse<EntityDefinitionDto>(dto));
        })
        .WithName("GetEntityDefinitionByRoute")
        .WithSummary("根据路由获取实体定义")
        .WithDescription("根据实体路由/名称获取实体定义（含字段、接口），支持系统实体")
        .Produces<SuccessResponse<EntityDefinitionDto>>()
        .Produces<ErrorResponse>(404)
        .AllowAnonymous();

        // 获取所有实体（包括禁用的）- 需要管理员权限
        entitiesGroup.MapGet("/all", async (string? lang, HttpContext http, AppDbContext db) =>
        {
            var targetLang = LangHelper.GetLang(http, lang);
            var entities = await db.EntityDefinitions
                .OrderBy(ed => ed.Order)
                .AsNoTracking()
                .ToListAsync();

            var dtos = entities
                .Select(ed =>
                {
                    var dto = ed.ToSummaryDto(targetLang);
                    // 保持原有行为：管理员视图 EntityType 使用 FullTypeName 便于调试
                    dto.EntityType = ed.FullTypeName ?? dto.EntityType;
                    return dto;
                })
                .ToList();

            return Results.Ok(new SuccessResponse<List<EntitySummaryDto>>(dtos));
        })
        .WithName("GetAllEntities")
        .WithSummary("获取所有实体列表（包括禁用的）")
        .WithDescription("管理员用：获取所有实体的元数据，包括已禁用的")
        .Produces<SuccessResponse<List<EntitySummaryDto>>>()
        .RequireAuthorization();

        // 验证实体路由是否有效
        entitiesGroup.MapGet("/{entityRoute}/validate", async (string entityRoute, AppDbContext db) =>
        {
            var entity = await db.EntityDefinitions
                .Where(ed => ed.EntityRoute == entityRoute && ed.IsEnabled && ed.Status == EntityStatus.Published)
                .FirstOrDefaultAsync();

            var isValid = entity != null;
            var entityPayload = entity == null
                ? null
                : new Dictionary<string, object?>
                {
                    ["id"] = entity.Id,
                    ["entityName"] = entity.EntityName,
                    ["entityRoute"] = entity.EntityRoute,
                    ["fullTypeName"] = entity.FullTypeName,
                    ["source"] = entity.Source,
                    ["status"] = entity.Status,
                    ["apiEndpoint"] = entity.ApiEndpoint,
                    ["displayName"] = entity.DisplayName
                };

            var response = new EntityRouteValidationResponse
            {
                IsValid = isValid,
                EntityRoute = entityRoute,
                Entity = entityPayload
            };

            return Results.Ok(new SuccessResponse<EntityRouteValidationResponse>(response));
        })
        .WithName("ValidateEntityRoute")
        .WithSummary("验证实体路由")
        .WithDescription("检查指定的实体路由是否存在且可用")
        .Produces<SuccessResponse<EntityRouteValidationResponse>>(StatusCodes.Status200OK)
        .AllowAnonymous();

        // ==================== 实体定义管理端点（需要认证）====================
        var group = app.MapGroup("/api/entity-definitions")
            .WithTags("实体定义管理")
            .WithOpenApi()
            .RequireAuthorization();

        // ==================== 查询 ====================

        // 获取所有实体定义列表
        group.MapGet("", async (string? lang, AppDbContext db, HttpContext http) =>
        {
            var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
            var definitions = await db.EntityDefinitions
                .Include(ed => ed.Fields)
                .Include(ed => ed.Interfaces)
                .OrderByDescending(ed => ed.UpdatedAt)
                .Select(ed => new EntityListDto
                {
                    Id = ed.Id,
                    Namespace = ed.Namespace,
                    EntityName = ed.EntityName,
                    FullTypeName = ed.FullTypeName,
                    EntityRoute = ed.EntityRoute,
                    DisplayName = targetLang != null ? ed.DisplayName.Resolve(targetLang) : null,
                    Description = targetLang != null ? ed.Description.Resolve(targetLang) : null,
                    DisplayNameTranslations = targetLang == null
                        ? (ed.DisplayName != null ? new MultilingualText(ed.DisplayName) : new MultilingualText())
                        : null,
                    DescriptionTranslations = targetLang == null
                        ? (ed.Description != null ? new MultilingualText(ed.Description) : null)
                        : null,
                    ApiEndpoint = ed.ApiEndpoint,
                    StructureType = ed.StructureType,
                    Status = ed.Status,
                    Source = ed.Source,
                    IsLocked = ed.IsLocked,
                    IsRootEntity = ed.IsRootEntity,
                    IsEnabled = ed.IsEnabled,
                    Order = ed.Order,
                    Icon = ed.Icon,
                    Category = ed.Category,
                    CreatedAt = ed.CreatedAt,
                    UpdatedAt = ed.UpdatedAt,
                    CreatedBy = ed.CreatedBy,
                    FieldCount = ed.Fields.Count,
                    Interfaces = ed.Interfaces.Select(i => new EntityInterfaceDto
                    {
                        Id = i.Id,
                        InterfaceType = i.InterfaceType,
                        IsEnabled = i.IsEnabled
                    }).ToList()
                })
                .ToListAsync();

            return Results.Ok(new SuccessResponse<List<EntityListDto>>(definitions));
        })
        .WithName("GetEntityDefinitions")
        .WithSummary("获取实体定义列表")
        .WithDescription("获取所有实体定义的列表，包括字段数量和接口类型")
        .Produces<SuccessResponse<List<EntityListDto>>>();

        // 获取单个实体定义详情
        group.MapGet("/{id:guid}", async (Guid id, string? lang, AppDbContext db, ILocalization loc, HttpContext http) =>
        {
            var uiLang = LangHelper.GetLang(http);
            var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
            var definition = await db.EntityDefinitions
                .Include(ed => ed.Fields.OrderBy(f => f.SortOrder))
                .Include(ed => ed.Interfaces)
                .FirstOrDefaultAsync(ed => ed.Id == id);

            if (definition == null)
                return Results.NotFound(new ErrorResponse(loc.T("ERR_ENTITY_NOT_FOUND", uiLang), "ENTITY_NOT_FOUND"));

            var dto = new EntityDefinitionDto
            {
                Id = definition.Id,
                Namespace = definition.Namespace,
                EntityName = definition.EntityName,
                FullTypeName = definition.FullTypeName,
                EntityRoute = definition.EntityRoute,
                DisplayName = targetLang != null ? definition.DisplayName.Resolve(targetLang) : null,
                Description = targetLang != null ? definition.Description.Resolve(targetLang) : null,
                DisplayNameTranslations = targetLang == null
                    ? (definition.DisplayName != null ? new MultilingualText(definition.DisplayName) : new MultilingualText())
                    : null,
                DescriptionTranslations = targetLang == null
                    ? (definition.Description != null ? new MultilingualText(definition.Description) : null)
                    : null,
                ApiEndpoint = definition.ApiEndpoint,
                StructureType = definition.StructureType,
                Status = definition.Status,
                Source = definition.Source,
                IsLocked = definition.IsLocked,
                IsRootEntity = definition.IsRootEntity,
                IsEnabled = definition.IsEnabled,
                Order = definition.Order,
                Icon = definition.Icon,
                Category = definition.Category,
                CreatedAt = definition.CreatedAt,
                UpdatedAt = definition.UpdatedAt,
                CreatedBy = definition.CreatedBy,
                UpdatedBy = definition.UpdatedBy,
                Fields = definition.Fields
                    .Where(f => !f.IsDeleted)
                    .OrderBy(f => f.SortOrder)
                    .Select(f => f.ToFieldDto(loc, targetLang))
                    .ToList(),
                Interfaces = definition.Interfaces.Select(i => new EntityInterfaceDto
                {
                    Id = i.Id,
                    InterfaceType = i.InterfaceType,
                    IsEnabled = i.IsEnabled
                }).ToList()
            };

            return Results.Ok(new SuccessResponse<EntityDefinitionDto>(dto));
        })
        .WithName("GetEntityDefinition")
        .WithSummary("获取实体定义详情")
        .WithDescription("获取指定实体定义的完整信息，包括所有字段、接口和多语言数据")
        .Produces<SuccessResponse<EntityDefinitionDto>>()
        .Produces<ErrorResponse>(404);

        // 根据实体类型名称获取实体定义（用于表单设计器）
        group.MapGet("/by-type/{entityType}", async (string entityType, string? lang, AppDbContext db, ILocalization loc, HttpContext http) =>
        {
            var uiLang = LangHelper.GetLang(http);
            var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
            var normalized = entityType?.Trim();

            var definition = await db.EntityDefinitions
                .Include(ed => ed.Fields.OrderBy(f => f.SortOrder))
                .Include(ed => ed.Interfaces)
                .FirstOrDefaultAsync(ed =>
                    ed.FullName == normalized ||
                    ed.FullTypeName == normalized ||
                    ed.EntityRoute == normalized ||
                    ed.EntityName == normalized);

            if (definition == null)
                return Results.NotFound(new ErrorResponse(loc.T("ERR_ENTITY_NOT_FOUND", uiLang), "ENTITY_NOT_FOUND"));

            var dto = new EntityDefinitionDto
            {
                Id = definition.Id,
                Namespace = definition.Namespace,
                EntityName = definition.EntityName,
                FullTypeName = definition.FullTypeName,
                EntityRoute = definition.EntityRoute,
                DisplayName = targetLang != null ? definition.DisplayName.Resolve(targetLang) : null,
                Description = targetLang != null ? definition.Description.Resolve(targetLang) : null,
                DisplayNameTranslations = targetLang == null
                    ? (definition.DisplayName != null ? new MultilingualText(definition.DisplayName) : new MultilingualText())
                    : null,
                DescriptionTranslations = targetLang == null
                    ? (definition.Description != null ? new MultilingualText(definition.Description) : null)
                    : null,
                ApiEndpoint = definition.ApiEndpoint,
                StructureType = definition.StructureType,
                Status = definition.Status,
                Source = definition.Source,
                IsLocked = definition.IsLocked,
                IsRootEntity = definition.IsRootEntity,
                IsEnabled = definition.IsEnabled,
                Order = definition.Order,
                Icon = definition.Icon,
                Category = definition.Category,
                CreatedAt = definition.CreatedAt,
                UpdatedAt = definition.UpdatedAt,
                CreatedBy = definition.CreatedBy,
                UpdatedBy = definition.UpdatedBy,
                Fields = definition.Fields
                    .Where(f => !f.IsDeleted)
                    .OrderBy(f => f.SortOrder)
                    .Select(f => f.ToFieldDto(loc, targetLang))
                    .ToList(),
                Interfaces = definition.Interfaces.Select(i => new EntityInterfaceDto
                {
                    Id = i.Id,
                    InterfaceType = i.InterfaceType,
                    IsEnabled = i.IsEnabled
                }).ToList()
            };

            return Results.Ok(new SuccessResponse<EntityDefinitionDto>(dto));
        })
        .WithName("GetEntityDefinitionByType")
        .WithSummary("根据类型名称获取实体定义")
        .WithDescription("根据实体的完整类型名称获取实体定义，用于表单设计器中的实体元数据树")
        .Produces<SuccessResponse<EntityDefinitionDto>>()
        .Produces<ErrorResponse>(404);

        // 检查实体是否被引用
        group.MapGet("/{id:guid}/referenced", async (Guid id, AppDbContext db, ILocalization loc, HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            var definition = await db.EntityDefinitions.FindAsync(id);

            if (definition == null)
                return Results.NotFound(new ErrorResponse(loc.T("ERR_ENTITY_NOT_FOUND", lang), "ENTITY_NOT_FOUND"));

            // 检查是否被FormTemplate引用
            var templateCount = await db.FormTemplates
                .Where(t => t.EntityType == definition.FullName)
                .CountAsync();

            return Results.Ok(new SuccessResponse<EntityReferenceCheckResponse>(new EntityReferenceCheckResponse
            {
                IsReferenced = templateCount > 0,
                ReferenceCount = templateCount,
                Details = new ReferenceDetailsDto
                {
                    FormTemplates = templateCount
                }
            }));
        })
        .WithName("CheckEntityReferenced")
        .WithSummary("检查实体是否被引用")
        .WithDescription("检查实体定义是否被模板或其他地方引用")
        .Produces<SuccessResponse<EntityReferenceCheckResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // ==================== 创建 ====================

        // 创建新实体定义
        // 创建新实体定义
        group.MapPost("", async (
            CreateEntityDefinitionDto dto,
            IEntityDefinitionAppService appService,
            HttpContext http,
            ILocalization loc,
            ILogger<Program> logger) =>
        {
            var lang = LangHelper.GetLang(http);
            var uid = http.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var result = await appService.CreateEntityDefinitionAsync(uid, lang, dto);
            return Results.Created($"/api/entity-definitions/{result.Id}", new SuccessResponse<EntityDefinitionDto>(result));
        })
        .WithName("CreateEntityDefinition")
        .WithSummary("创建实体定义")
        .WithDescription("创建新的实体定义，可包含字段和接口")
        .Produces<SuccessResponse<EntityDefinitionDto>>(201)
        .Produces<ErrorResponse>(400)
        .Produces<ErrorResponse>(409);

        // ==================== 更新 ====================

        // 更新实体定义
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateEntityDefinitionDto dto,
            IEntityDefinitionAppService appService,
            HttpContext http,
            ILocalization loc,
            ILogger<Program> logger) =>
        {
            var lang = LangHelper.GetLang(http);
            var uid = http.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var result = await appService.UpdateEntityDefinitionAsync(id, uid, lang, dto);
            return Results.Ok(new SuccessResponse<EntityDefinitionDto>(result));
        })
        .WithName("UpdateEntityDefinition")
        .WithSummary("更新实体定义")
        .WithDescription("更新实体定义，如果实体已被引用则受到限制")
        .Produces<SuccessResponse<EntityDefinitionDto>>()
        .Produces<ErrorResponse>(400)
        .Produces<ErrorResponse>(404);

        // ==================== 删除 ====================

        // 删除实体定义
        group.MapDelete("/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ILocalization loc,
            HttpContext http,
            ILogger<Program> logger) =>
        {
            var lang = LangHelper.GetLang(http);
            var definition = await db.EntityDefinitions
                .Include(ed => ed.Fields)
                .Include(ed => ed.Interfaces)
                .FirstOrDefaultAsync(ed => ed.Id == id);

            if (definition == null)
                return Results.NotFound(new ErrorResponse(loc.T("ERR_ENTITY_NOT_FOUND", lang), "ENTITY_NOT_FOUND"));

            // 检查是否已发布
            if (definition.Status == EntityStatus.Published)
            {
                logger.LogWarning("[EntityDefinition] Cannot delete published entity: {Id}", id);
                return Results.BadRequest(new ErrorResponse(loc.T("ERR_ENTITY_PUBLISHED_DELETE", lang), "ENTITY_PUBLISHED"));
            }

            // 检查是否被引用
            var templateCount = await db.FormTemplates
                .Where(t => t.EntityType == definition.FullName)
                .CountAsync();

            if (templateCount > 0)
            {
                logger.LogWarning("[EntityDefinition] Cannot delete referenced entity: {Id}, TemplateCount={Count}",
                    id, templateCount);
                return Results.BadRequest(new ErrorResponse(loc.T("ERR_ENTITY_REFERENCED_DELETE", lang), "ENTITY_REFERENCED"));
            }

            db.EntityDefinitions.Remove(definition);
            await db.SaveChangesAsync();

            logger.LogInformation("[EntityDefinition] Entity deleted successfully: {Id}", id);

            return Results.Ok(new SuccessResponse(loc.T("MSG_ENTITY_DELETE_SUCCESS", lang)));
        })
        .WithName("DeleteEntityDefinition")
        .WithSummary("删除实体定义")
        .WithDescription("删除实体定义（仅限草稿状态且未被引用）")
        .Produces<SuccessResponse>()
        .Produces<ErrorResponse>(400)
        .Produces<ErrorResponse>(404);

        // ==================== 发布 ====================

        // 发布新实体（CREATE TABLE）
        group.MapPost("/{id:guid}/publish", async (
            Guid id,
            AppDbContext db,
            Services.IEntityPublishingService publishService,
            BobCrm.Api.Abstractions.IBackgroundJobClient jobs,
            HttpContext http,
            ILocalization loc,
            ILogger<Program> logger) =>
        {
            var lang = LangHelper.GetLang(http);
            var uid = http.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            logger.LogInformation("[Publish] Publishing new entity: {Id}", id);

            var jobId = jobs.StartJob($"Publish entity {id}", "EntityPublishing", uid, http.User?.Identity?.Name, canCancel: false);
            jobs.AppendLog(jobId, "INFO", $"Publishing new entity {id}.");

            var result = await publishService.PublishNewEntityAsync(id, uid);

            if (!result.Success)
            {
                logger.LogError("[Publish] Failed: {Error}", result.ErrorMessage);
                jobs.Fail(jobId, result.ErrorMessage ?? "Publish failed");
                return Results.BadRequest(new ErrorResponse(result.ErrorMessage ?? loc.T("ERR_PUBLISH_FAILED", lang), "PUBLISH_FAILED"));
            }

            jobs.Complete(jobId);
            return Results.Ok(new SuccessResponse<PublishResultDto>(new PublishResultDto
            {
                Success = true,
                ScriptId = result.ScriptId,
                DdlScript = result.DDLScript,
                Templates = result.Templates.Select(t => new TemplateInfoDto
                {
                    ViewState = t.ViewState,
                    TemplateId = t.TemplateId,
                    TemplateName = t.TemplateName
                }),
                Bindings = result.TemplateBindings.Select(b => new TemplateBindingInfoDto
                {
                    ViewState = b.ViewState,
                    Usage = b.UsageType.ToString(),
                    UsageType = b.UsageType.ToString(),
                    BindingId = b.BindingId,
                    TemplateId = b.TemplateId,
                    RequiredPermission = b.RequiredFunctionCode
                }),
                Menus = result.MenuNodes.Select(m => new MenuNodeInfoDto
                {
                    Code = m.Code,
                    NodeId = m.NodeId,
                    ParentId = m.ParentId,
                    Route = m.Route,
                    ViewState = m.ViewState,
                    Usage = m.UsageType.ToString(),
                    UsageType = m.UsageType.ToString()
                }),
                Message = loc.T("MSG_ENTITY_PUBLISH_SUCCESS", lang)
            }));
        })
        .WithName("PublishEntity")
        .WithSummary("发布新实体")
        .WithDescription("发布新实体定义，生成并执行CREATE TABLE语句")
        .Produces<SuccessResponse<PublishResultDto>>()
        .Produces<ErrorResponse>(400);

        // 发布实体修改（ALTER TABLE）
        group.MapPost("/{id:guid}/publish-changes", async (
            Guid id,
            AppDbContext db,
            Services.IEntityPublishingService publishService,
            BobCrm.Api.Abstractions.IBackgroundJobClient jobs,
            HttpContext http,
            ILocalization loc,
            ILogger<Program> logger) =>
        {
            var lang = LangHelper.GetLang(http);
            var uid = http.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            logger.LogInformation("[Publish] Publishing changes for entity: {Id}", id);

            var jobId = jobs.StartJob($"Publish entity changes {id}", "EntityPublishing", uid, http.User?.Identity?.Name, canCancel: false);
            jobs.AppendLog(jobId, "INFO", $"Publishing changes for entity {id}.");

            var result = await publishService.PublishEntityChangesAsync(id, uid);

            if (!result.Success)
            {
                logger.LogError("[Publish] Failed: {Error}", result.ErrorMessage);
                jobs.Fail(jobId, result.ErrorMessage ?? "Publish changes failed");
                return Results.BadRequest(new ErrorResponse(result.ErrorMessage ?? loc.T("ERR_PUBLISH_FAILED", lang), "PUBLISH_FAILED"));
            }

            jobs.Complete(jobId);
            return Results.Ok(new SuccessResponse<PublishResultDto>(new PublishResultDto
            {
                Success = true,
                ScriptId = result.ScriptId,
                DdlScript = result.DDLScript,
                ChangeAnalysis = new ChangeAnalysisDto
                {
                    NewFieldsCount = result.ChangeAnalysis?.NewFields.Count ?? 0,
                    LengthIncreasesCount = result.ChangeAnalysis?.LengthIncreases.Count ?? 0,
                    HasDestructiveChanges = result.ChangeAnalysis?.HasDestructiveChanges ?? false
                },
                Templates = result.Templates.Select(t => new TemplateInfoDto
                {
                    ViewState = t.ViewState,
                    TemplateId = t.TemplateId,
                    TemplateName = t.TemplateName
                }),
                Bindings = result.TemplateBindings.Select(b => new TemplateBindingInfoDto
                {
                    ViewState = b.ViewState,
                    Usage = b.UsageType.ToString(),
                    UsageType = b.UsageType.ToString(),
                    BindingId = b.BindingId,
                    TemplateId = b.TemplateId,
                    RequiredPermission = b.RequiredFunctionCode
                }),
                Menus = result.MenuNodes.Select(m => new MenuNodeInfoDto
                {
                    Code = m.Code,
                    NodeId = m.NodeId,
                    ParentId = m.ParentId,
                    Route = m.Route,
                    ViewState = m.ViewState,
                    Usage = m.UsageType.ToString(),
                    UsageType = m.UsageType.ToString()
                }),
                Message = loc.T("MSG_ENTITY_CHANGE_PUBLISH_SUCCESS", lang)
            }));
        })
        .WithName("PublishEntityChanges")
        .WithSummary("发布实体修改")
        .WithDescription("发布实体定义的修改，生成并执行ALTER TABLE语句")
        .Produces<SuccessResponse<PublishResultDto>>()
        .Produces<ErrorResponse>(400);

        // 撤回发布（DROP TABLE 或逻辑撤回）
        group.MapPost("/{id:guid}/withdraw", async (
            Guid id,
            Services.IEntityPublishingService publishService,
            BobCrm.Api.Abstractions.IBackgroundJobClient jobs,
            HttpContext http,
            ILocalization loc,
            ILogger<Program> logger) =>
        {
            var lang = LangHelper.GetLang(http);
            var uid = http.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            logger.LogInformation("[Withdraw] Withdrawing entity: {Id}", id);

            var jobId = jobs.StartJob($"Withdraw entity {id}", "EntityPublishing", uid, http.User?.Identity?.Name, canCancel: false);
            jobs.AppendLog(jobId, "INFO", $"Withdrawing entity {id}.");

            var result = await publishService.WithdrawAsync(id, uid);
            if (!result.Success)
            {
                logger.LogError("[Withdraw] Failed: {Error}", result.ErrorMessage);
                jobs.Fail(jobId, result.ErrorMessage ?? "Withdraw failed");
                return Results.BadRequest(new ErrorResponse(result.ErrorMessage ?? "Withdraw failed", "WITHDRAW_FAILED"));
            }

            jobs.Complete(jobId);
            return Results.Ok(new SuccessResponse<WithdrawResultDto>(new WithdrawResultDto
            {
                Success = true,
                ScriptId = result.ScriptId,
                DdlScript = result.DDLScript,
                Mode = result.Mode,
                Message = loc.T("MSG_ENTITY_WITHDRAW_SUCCESS", lang)
            }));
        })
        .WithName("WithdrawEntity")
        .WithSummary("撤回发布")
        .WithDescription("撤回已发布实体：逻辑撤回或物理删除表（受配置控制）")
        .Produces<SuccessResponse<WithdrawResultDto>>()
        .Produces<ErrorResponse>(400);

        // 预览DDL脚本（不执行）
        group.MapGet("/{id:guid}/preview-ddl", async (
            Guid id,
            AppDbContext db,
            Services.PostgreSQLDDLGenerator ddlGenerator,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            var definition = await db.EntityDefinitions
                .Include(ed => ed.Fields.OrderBy(f => f.SortOrder))
                .Include(ed => ed.Interfaces)
                .FirstOrDefaultAsync(ed => ed.Id == id);

            if (definition == null)
                return Results.NotFound(new ErrorResponse(loc.T("ERR_ENTITY_NOT_FOUND", lang), "ENTITY_NOT_FOUND"));

            string ddlScript;

            if (definition.Status == EntityStatus.Draft)
            {
                ddlScript = ddlGenerator.GenerateCreateTableScript(definition);
            }
            else if (definition.Status == EntityStatus.Modified)
            {
                // 简化实现：只显示添加字段的脚本
                var newFields = definition.Fields.Where(f => f.CreatedAt > definition.UpdatedAt.AddMinutes(-5)).ToList();
                if (newFields.Any())
                {
                    ddlScript = ddlGenerator.GenerateAlterTableAddColumns(definition, newFields);
                }
                else
                {
                    ddlScript = loc.T("MSG_NO_CHANGES", lang);
                }
            }
            else
            {
                ddlScript = loc.T("MSG_NO_PENDING_CHANGES", lang);
            }

            return Results.Ok(new SuccessResponse<PreviewDdlResultDto>(new PreviewDdlResultDto
            {
                EntityId = id,
                EntityName = definition.EntityName,
                Status = definition.Status,
                DdlScript = ddlScript
            }));
        })
        .WithName("PreviewDDL")
        .WithSummary("预览DDL脚本")
        .WithDescription("预览将要执行的DDL脚本（不实际执行）")
        .Produces<SuccessResponse<PreviewDdlResultDto>>()
        .Produces<ErrorResponse>(404);

        // 获取DDL执行历史
        group.MapGet("/{id:guid}/ddl-history", async (
            Guid id,
            AppDbContext db,
            Services.DDLExecutionService ddlExecutor) =>
        {
            var history = await ddlExecutor.GetDDLHistoryAsync(id);

            var dtos = history.Select(h => new DdlExecutionHistoryDto
            {
                Id = h.Id,
                ScriptType = h.ScriptType,
                Status = h.Status,
                CreatedAt = h.CreatedAt,
                ExecutedAt = h.ExecutedAt,
                CreatedBy = h.CreatedBy,
                ErrorMessage = h.ErrorMessage,
                ScriptPreview = h.SqlScript.Length > 200 ? h.SqlScript.Substring(0, 200) + "..." : h.SqlScript
            }).ToList();

            return Results.Ok(new SuccessResponse<List<DdlExecutionHistoryDto>>(dtos));
        })
        .WithName("GetDDLHistory")
        .WithSummary("获取DDL执行历史")
        .WithDescription("获取实体定义的所有DDL脚本执行历史")
        .Produces<SuccessResponse<List<DdlExecutionHistoryDto>>>();

        // ==================== 代码生成与编译 ====================

        // 生成实体代码
        group.MapGet("/{id:guid}/generate-code", async (
            Guid id,
            AppDbContext db,
            Services.DynamicEntityService dynamicEntityService,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            try
            {
                var code = await dynamicEntityService.GenerateCodeAsync(id);
                return Results.Ok(new SuccessResponse<CodeGenerationResultDto>(new CodeGenerationResultDto
                {
                    EntityId = id,
                    Code = code,
                    Message = loc.T("MSG_CODE_GENERATION_SUCCESS", lang)
                }));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ErrorResponse(
                    string.Format(loc.T("ERR_CODE_GENERATION_FAILED", lang), ex.Message),
                    "CODE_GENERATION_FAILED"));
            }
        })
        .WithName("GenerateEntityCode")
        .WithSummary("生成实体代码")
        .WithDescription("生成实体的C#类代码（不编译）")
        .Produces<SuccessResponse<CodeGenerationResultDto>>()
        .Produces<ErrorResponse>(400);

        // 编译实体
        group.MapPost("/{id:guid}/compile", async (
            Guid id,
            AppDbContext db,
            Services.DynamicEntityService dynamicEntityService,
            ILocalization loc,
            HttpContext http,
            ILogger<Program> logger) =>
        {
            var lang = LangHelper.GetLang(http);
            try
            {
                logger.LogInformation("[Compile] Compiling entity: {Id}", id);
                var result = await dynamicEntityService.CompileEntityAsync(id);

                if (!result.Success)
                {
                    logger.LogError("[Compile] Failed with {Count} errors", result.Errors.Count);
                    return Results.BadRequest(new ErrorResponse(loc.T("ERR_COMPILE_FAILED", lang), new Dictionary<string, string[]>
                    {
                        { "Errors", result.Errors.Select(e => $"{e.Code}: {e.Message} at {e.Line}:{e.Column}").ToArray() }
                    }, "COMPILE_FAILED"));
                }

                return Results.Ok(new SuccessResponse<CompileResultDto>(new CompileResultDto
                {
                    Success = true,
                    AssemblyName = result.AssemblyName,
                    LoadedTypes = result.LoadedTypes,
                    Message = loc.T("MSG_ENTITY_COMPILE_SUCCESS", lang)
                }));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Compile] Exception: {Message}", ex.Message);
                return Results.BadRequest(new ErrorResponse(
                    string.Format(loc.T("ERR_COMPILE_FAILED_DETAIL", lang), ex.Message),
                    "COMPILE_FAILED"));
            }
        })
        .WithName("CompileEntity")
        .WithSummary("编译实体")
        .WithDescription("编译实体代码并加载到内存")
        .Produces<SuccessResponse<CompileResultDto>>()
        .Produces<ErrorResponse>(400);

        // 批量编译实体
        group.MapPost("/compile-batch", async (
            CompileBatchDto dto,
            AppDbContext db,
            Services.DynamicEntityService dynamicEntityService,
            ILocalization loc,
            HttpContext http,
            ILogger<Program> logger) =>
        {
            var lang = LangHelper.GetLang(http);
            try
            {
                logger.LogInformation("[Compile] Batch compiling {Count} entities", dto.EntityIds.Count);
                var result = await dynamicEntityService.CompileMultipleEntitiesAsync(dto.EntityIds);

                if (!result.Success)
                {
                    logger.LogError("[Compile] Batch failed with {Count} errors", result.Errors.Count);
                    return Results.BadRequest(new ErrorResponse(loc.T("ERR_COMPILE_BATCH_FAILED", lang), new Dictionary<string, string[]>
                    {
                        { "Errors", result.Errors.Select(e => $"{e.Code}: {e.Message} at {e.FilePath}:{e.Line}").ToArray() }
                    }, "COMPILE_FAILED"));
                }

                return Results.Ok(new SuccessResponse<CompileResultDto>(new CompileResultDto
                {
                    Success = true,
                    AssemblyName = result.AssemblyName,
                    LoadedTypes = result.LoadedTypes,
                    Count = result.LoadedTypes.Count,
                    Message = string.Format(loc.T("MSG_COMPILE_BATCH_SUCCESS", lang), result.LoadedTypes.Count)
                }));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Compile] Batch exception: {Message}", ex.Message);
                return Results.BadRequest(new ErrorResponse(
                    string.Format(loc.T("ERR_COMPILE_FAILED_DETAIL", lang), ex.Message),
                    "COMPILE_FAILED"));
            }
        })
        .WithName("CompileBatchEntities")
        .WithSummary("批量编译实体")
        .WithDescription("批量编译多个实体到同一程序集")
        .Produces<SuccessResponse<CompileResultDto>>()
        .Produces<ErrorResponse>(400);

        // 验证实体代码语法
        group.MapGet("/{id:guid}/validate-code", async (
            Guid id,
            AppDbContext db,
            Services.DynamicEntityService dynamicEntityService,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            try
            {
                var result = await dynamicEntityService.ValidateEntityCodeAsync(id);
                return Results.Ok(new SuccessResponse<ValidateCodeResultDto>(new ValidateCodeResultDto
                {
                    IsValid = result.IsValid,
                    Errors = result.Errors.Select(e => new CompileErrorDto
                    {
                        Code = e.Code,
                        Message = e.Message,
                        Line = e.Line,
                        Column = e.Column
                    })
                }));
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ErrorResponse(
                    string.Format(loc.T("ERR_VALIDATION_FAILED_DETAIL", lang), ex.Message),
                    "VALIDATION_FAILED"));
            }
        })
        .WithName("ValidateEntityCode")
        .WithSummary("验证实体代码")
        .WithDescription("验证生成的实体代码语法（不编译）")
        .Produces<SuccessResponse<ValidateCodeResultDto>>()
        .Produces<ErrorResponse>(400);

        // 获取已加载的实体列表
        group.MapGet("/loaded-entities", (Services.DynamicEntityService dynamicEntityService) =>
        {
            var loadedEntities = dynamicEntityService.GetLoadedEntities();
            return Results.Ok(new SuccessResponse<LoadedEntitiesDto>(new LoadedEntitiesDto
            {
                Count = loadedEntities.Count,
                Entities = loadedEntities
            }));
        })
        .WithName("GetLoadedEntities")
        .WithSummary("获取已加载实体")
        .WithDescription("获取当前已加载到内存的所有动态实体")
        .Produces<SuccessResponse<LoadedEntitiesDto>>();

        // 获取实体类型信息
        group.MapGet("/type-info/{fullTypeName}", (
            string fullTypeName,
            Services.DynamicEntityService dynamicEntityService,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            var typeInfo = dynamicEntityService.GetEntityTypeInfo(fullTypeName);
            if (typeInfo == null)
                return Results.NotFound(new ErrorResponse(loc.T("ERR_TYPE_NOT_LOADED", lang), "TYPE_NOT_LOADED"));

            var dto = new EntityTypeInfoDto
            {
                FullName = typeInfo.FullName,
                Name = typeInfo.Name,
                Namespace = typeInfo.Namespace,
                IsLoaded = typeInfo.IsLoaded,
                Properties = typeInfo.Properties
                    .Select(p => new PropertyTypeInfoDto
                    {
                        Name = p.Name,
                        TypeName = p.TypeName,
                        IsNullable = p.IsNullable,
                        CanRead = p.CanRead,
                        CanWrite = p.CanWrite
                    })
                    .ToList(),
                Interfaces = typeInfo.Interfaces
            };

            return Results.Ok(new SuccessResponse<EntityTypeInfoDto>(dto));
        })
        .WithName("GetEntityTypeInfo")
        .WithSummary("获取实体类型信息")
        .WithDescription("获取已加载实体的类型元数据信息")
        .Produces<SuccessResponse<EntityTypeInfoDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // 卸载实体
        group.MapDelete("/loaded-entities/{fullTypeName}", (
            string fullTypeName,
            Services.DynamicEntityService dynamicEntityService,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            dynamicEntityService.UnloadEntity(fullTypeName);
            return Results.Ok(new SuccessResponse(string.Format(loc.T("MSG_ENTITY_UNLOADED", lang), fullTypeName)));
        })
        .WithName("UnloadEntity")
        .WithSummary("卸载实体")
        .WithDescription("从内存中卸载指定的动态实体")
        .Produces<SuccessResponse>();

        // 重新编译实体
        group.MapPost("/{id:guid}/recompile", async (
            Guid id,
            AppDbContext db,
            Services.DynamicEntityService dynamicEntityService,
            ILocalization loc,
            HttpContext http,
            ILogger<Program> logger) =>
        {
            var lang = LangHelper.GetLang(http);
            try
            {
                logger.LogInformation("[Recompile] Recompiling entity: {Id}", id);
                var result = await dynamicEntityService.RecompileEntityAsync(id);

                if (!result.Success)
                {
                    return Results.BadRequest(new ErrorResponse(loc.T("ERR_RECOMPILE_FAILED", lang), new Dictionary<string, string[]>
                    {
                        { "Errors", result.Errors.Select(e => $"{e.Code}: {e.Message}").ToArray() }
                    }, "COMPILE_FAILED"));
                }

                return Results.Ok(new SuccessResponse<CompileResultDto>(new CompileResultDto
                {
                    Success = true,
                    AssemblyName = result.AssemblyName,
                    LoadedTypes = result.LoadedTypes,
                    Message = loc.T("MSG_ENTITY_RECOMPILE_SUCCESS", lang)
                }));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Recompile] Exception: {Message}", ex.Message);
                return Results.BadRequest(new ErrorResponse(
                    string.Format(loc.T("ERR_COMPILE_FAILED_DETAIL", lang), ex.Message),
                    "COMPILE_FAILED"));
            }
        })
        .WithName("RecompileEntity")
        .WithSummary("重新编译实体")
        .WithDescription("卸载并重新编译实体（用于实体定义更新后）")
        .Produces<SuccessResponse<CompileResultDto>>()
        .Produces<ErrorResponse>(400);

        return app;
    }
}
