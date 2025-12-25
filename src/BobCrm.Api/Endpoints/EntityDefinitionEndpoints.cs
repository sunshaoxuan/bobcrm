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
        group.MapPost("", async (
            CreateEntityDefinitionDto dto,
            AppDbContext db,
            HttpContext http,
            ILocalization loc,
            ILogger<Program> logger) =>
        {
            var lang = LangHelper.GetLang(http);
            var uid = http.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            logger.LogInformation("[EntityDefinition] Creating new entity: {Namespace}.{EntityName}",
                dto.Namespace, dto.EntityName);

            // 验证必填字段
            if (string.IsNullOrWhiteSpace(dto.Namespace))
                return Results.BadRequest(new ErrorResponse(loc.T("ERR_NAMESPACE_REQUIRED", lang), "VALIDATION_ERROR"));

            if (string.IsNullOrWhiteSpace(dto.EntityName))
                return Results.BadRequest(new ErrorResponse(loc.T("ERR_ENTITY_NAME_REQUIRED", lang), "VALIDATION_ERROR"));

            // 验证多语言显示名
            if (dto.DisplayName == null || !dto.DisplayName.Any() ||
                !dto.DisplayName.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
            {
                return Results.BadRequest(new ErrorResponse(loc.T("ERR_DISPLAY_NAME_REQUIRED", lang), "VALIDATION_ERROR"));
            }

            // 检查是否已存在同名实体
            var exists = await db.EntityDefinitions
                .AnyAsync(ed => ed.Namespace == dto.Namespace && ed.EntityName == dto.EntityName);

            if (exists)
            {
                logger.LogWarning("[EntityDefinition] Entity already exists: {Namespace}.{EntityName}",
                    dto.Namespace, dto.EntityName);
                return Results.Conflict(new ErrorResponse(loc.T("ERR_ENTITY_EXISTS", lang), "ENTITY_EXISTS"));
            }

            // 创建实体定义（直接保存 jsonb 多语言数据）
            var definition = new EntityDefinition
            {
                Namespace = dto.Namespace,
                EntityName = dto.EntityName,
                DisplayName = dto.DisplayName,  // 直接赋值 Dictionary，EF Core 自动转 jsonb
                Description = dto.Description?.Any(kvp => !string.IsNullOrWhiteSpace(kvp.Value)) == true
                    ? dto.Description
                    : null,
                StructureType = string.IsNullOrWhiteSpace(dto.StructureType) ? EntityStructureType.Single : dto.StructureType,
                Status = EntityStatus.Draft,
                CreatedBy = uid,
                UpdatedBy = uid
            };

            // 添加字段
            if (dto.Fields != null)
            {
                foreach (var fieldDto in dto.Fields)
                {
                    // 验证字段显示名
                    if (fieldDto.DisplayName == null || !fieldDto.DisplayName.Any() ||
                        !fieldDto.DisplayName.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
                    {
                        var message = string.Format(loc.T("ERR_FIELD_DISPLAY_NAME_REQUIRED", lang), fieldDto.PropertyName);
                        return Results.BadRequest(new ErrorResponse(message, "VALIDATION_ERROR"));
                    }

                    definition.Fields.Add(new FieldMetadata
                    {
                        PropertyName = fieldDto.PropertyName,
                        DisplayName = fieldDto.DisplayName,  // 直接赋值 Dictionary，EF Core 自动转 jsonb
                        DataType = fieldDto.DataType,
                        Length = fieldDto.Length,
                        Precision = fieldDto.Precision,
                        Scale = fieldDto.Scale,
                        IsRequired = fieldDto.IsRequired,
                        IsEntityRef = fieldDto.IsEntityRef,
                        ReferencedEntityId = fieldDto.ReferencedEntityId,
                        LookupEntityName = fieldDto.LookupEntityName,
                        LookupDisplayField = fieldDto.LookupDisplayField,
                        ForeignKeyAction = fieldDto.ForeignKeyAction,
                        SortOrder = fieldDto.SortOrder,
                        DefaultValue = fieldDto.DefaultValue,
                        ValidationRules = fieldDto.ValidationRules
                    });
                }
            }

            // 添加接口
            if (dto.Interfaces != null)
            {
                foreach (var interfaceType in dto.Interfaces)
                {
                    definition.Interfaces.Add(new EntityInterface
                    {
                        InterfaceType = interfaceType,
                        IsEnabled = true
                    });

                    // 根据接口类型自动添加字段
                    var interfaceFields = InterfaceFieldMapping.GetFields(interfaceType);
                    foreach (var ifField in interfaceFields)
                    {
                        // 检查字段是否已存在
                        if (!definition.Fields.Any(f => f.PropertyName == ifField.PropertyName))
                        {
                            definition.Fields.Add(new FieldMetadata
                            {
                                PropertyName = ifField.PropertyName,
                                DisplayName = null,  // 接口字段暂不提供默认多语言
                                DataType = ifField.DataType,
                                Length = ifField.Length,
                                IsRequired = ifField.IsRequired,
                                IsEntityRef = ifField.IsEntityRef,
                                TableName = ifField.ReferenceTable,
                                DefaultValue = ifField.DefaultValue,
                                SortOrder = 0 // 接口字段排在前面
                            });
                        }
                    }
                }
            }

            db.EntityDefinitions.Add(definition);
            await db.SaveChangesAsync();

            logger.LogInformation("[EntityDefinition] Entity created successfully: {Id}", definition.Id);

            // Return DTO
            var resultDto = new EntityDefinitionDto
            {
                Id = definition.Id,
                Namespace = definition.Namespace,
                EntityName = definition.EntityName,
                FullTypeName = definition.FullTypeName,
                EntityRoute = definition.EntityRoute,
                DisplayNameTranslations = definition.DisplayName != null
                    ? new MultilingualText(definition.DisplayName)
                    : new MultilingualText(),
                DescriptionTranslations = definition.Description != null
                    ? new MultilingualText(definition.Description)
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
                UpdatedBy = definition.UpdatedBy
            };

            return Results.Created($"/api/entity-definitions/{definition.Id}", new SuccessResponse<EntityDefinitionDto>(resultDto));
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
            AppDbContext db,
            HttpContext http,
            ILocalization loc,
            ILogger<Program> logger) =>
        {
            var lang = LangHelper.GetLang(http);
            var uid = http.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var definition = await db.EntityDefinitions
                .Include(ed => ed.Fields)
                .Include(ed => ed.Interfaces)
                .FirstOrDefaultAsync(ed => ed.Id == id);

            if (definition == null)
                return Results.NotFound(new ErrorResponse(loc.T("ERR_ENTITY_NOT_FOUND", lang), "ENTITY_NOT_FOUND"));

            logger.LogInformation("[EntityDefinition] Updating entity: {Id}, IsLocked={IsLocked}",
                id, definition.IsLocked);

            // 如果实体已被引用锁定，则进行严格校验
            if (definition.IsLocked)
            {
                // 不允许修改命名空间和实体名
                if (dto.Namespace != null && dto.Namespace != definition.Namespace)
                    return Results.BadRequest(new ErrorResponse(loc.T("ERR_ENTITY_LOCKED_NAMESPACE", lang), "ENTITY_LOCKED"));

                if (dto.EntityName != null && dto.EntityName != definition.EntityName)
                    return Results.BadRequest(new ErrorResponse(loc.T("ERR_ENTITY_LOCKED_NAME", lang), "ENTITY_LOCKED"));

                // 检查字段删除
                if (dto.Fields != null)
                {
                    var existingFieldIds = definition.Fields.Select(f => f.Id).ToHashSet();
                    var newFieldIds = dto.Fields.Select(f => f.Id).Where(id => id.HasValue).Select(id => id!.Value).ToHashSet();
                    var deletedFieldIds = existingFieldIds.Except(newFieldIds).ToList();

                    if (deletedFieldIds.Any())
                    {
                        logger.LogWarning("[EntityDefinition] Cannot delete fields when entity is locked: {FieldIds}",
                            string.Join(", ", deletedFieldIds));
                        return Results.BadRequest(new ErrorResponse(loc.T("ERR_ENTITY_LOCKED_DELETE_FIELD", lang), "ENTITY_LOCKED"));
                    }

                    // 检查字段类型修改和长度缩小
                    foreach (var fieldDto in dto.Fields.Where(f => f.Id.HasValue))
                    {
                        var existingField = definition.Fields.FirstOrDefault(f => f.Id == fieldDto.Id!.Value);
                        if (existingField != null)
                        {
                            if (fieldDto.DataType != null && fieldDto.DataType != existingField.DataType)
                                return Results.BadRequest(new ErrorResponse(string.Format(loc.T("ERR_FIELD_LOCKED_TYPE_CHANGE", lang), existingField.PropertyName), "ENTITY_LOCKED"));

                            if (fieldDto.Length.HasValue && fieldDto.Length < existingField.Length)
                                return Results.BadRequest(new ErrorResponse(string.Format(loc.T("ERR_FIELD_LOCKED_LENGTH_DECREASE", lang), existingField.PropertyName), "ENTITY_LOCKED"));
                        }
                    }
                }

                // 检查接口删除
                if (dto.Interfaces != null)
                {
                    var existingInterfaceTypes = definition.Interfaces.Where(i => i.IsLocked).Select(i => i.InterfaceType).ToHashSet();
                    var removedInterfaces = existingInterfaceTypes.Except(dto.Interfaces).ToList();

                    if (removedInterfaces.Any())
                    {
                        logger.LogWarning("[EntityDefinition] Cannot remove locked interfaces: {Interfaces}",
                            string.Join(", ", removedInterfaces));
                        return Results.BadRequest(new ErrorResponse(loc.T("ERR_ENTITY_LOCKED_DELETE_INTERFACE", lang), "ENTITY_LOCKED"));
                    }
                }
            }

            // 更新基本信息
            if (dto.Namespace != null) definition.Namespace = dto.Namespace;
            if (dto.EntityName != null) definition.EntityName = dto.EntityName;
            if (dto.StructureType != null) definition.StructureType = dto.StructureType;

            // 更新多语言显示名（直接更新 jsonb 字段）
            if (dto.DisplayName != null && dto.DisplayName.Any() &&
                dto.DisplayName.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
            {
                definition.DisplayName = dto.DisplayName;  // EF Core 自动更新 jsonb
            }

            // 更新多语言描述（直接更新 jsonb 字段）
            if (dto.Description != null && dto.Description.Any() &&
                dto.Description.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
            {
                definition.Description = dto.Description;  // EF Core 自动更新 jsonb
            }

            definition.UpdatedAt = DateTime.UtcNow;
            definition.UpdatedBy = uid;

            // 如果状态是Published，改为Modified
            if (definition.Status == EntityStatus.Published)
                definition.Status = EntityStatus.Modified;

            // 更新字段列表
            if (dto.Fields != null)
            {
                // 获取现有字段ID集合
                var existingFieldIds = definition.Fields.Select(f => f.Id).ToHashSet();
                var incomingFieldIds = dto.Fields.Where(f => f.Id.HasValue).Select(f => f.Id!.Value).ToHashSet();

                // 软删除不在新列表中的字段
                var fieldsToRemove = definition.Fields.Where(f => !incomingFieldIds.Contains(f.Id) && !f.IsDeleted).ToList();
                foreach (var field in fieldsToRemove)
                {
                    // System字段和Interface字段不能删除
                    if (field.Source == FieldSource.System)
                    {
                        logger.LogWarning("[EntityDefinition] Cannot delete System field: {FieldName}", field.PropertyName);
                        return Results.BadRequest(new ErrorResponse(string.Format(loc.T("ERR_SYSTEM_FIELD_DELETE", lang), field.PropertyName), "VALIDATION_ERROR"));
                    }

                    if (field.Source == FieldSource.Interface)
                    {
                        logger.LogWarning("[EntityDefinition] Cannot delete Interface field: {FieldName}", field.PropertyName);
                        return Results.BadRequest(new ErrorResponse(string.Format(loc.T("ERR_INTERFACE_FIELD_DELETE", lang), field.PropertyName), "VALIDATION_ERROR"));
                    }

                    // Custom字段：软删除（标记为已删除）
                    logger.LogInformation("[EntityDefinition] Soft deleting field: {FieldName}", field.PropertyName);
                    field.IsDeleted = true;
                    field.DeletedAt = DateTime.UtcNow;
                    field.DeletedBy = uid;
                }

                // 更新或添加字段
                foreach (var fieldDto in dto.Fields)
                {
                    // 检查字段是否存在于数据库中（通过检查是否在现有字段集合中）
                    var existingField = fieldDto.Id.HasValue
                        ? definition.Fields.FirstOrDefault(f => f.Id == fieldDto.Id.Value)
                        : null;

                    if (existingField != null)
                    {
                        // 更新现有字段
                        // System字段和Interface字段的某些属性受保护，不能修改
                        if (existingField.Source == FieldSource.System)
                        {
                            // System字段只允许更新显示名和排序
                            if (fieldDto.DisplayName != null) existingField.DisplayName = fieldDto.DisplayName;
                            if (fieldDto.SortOrder.HasValue) existingField.SortOrder = fieldDto.SortOrder.Value;
                        }
                        else if (existingField.Source == FieldSource.Interface)
                        {
                            // Interface字段允许更新显示名、排序和默认值
                            if (fieldDto.DisplayName != null) existingField.DisplayName = fieldDto.DisplayName;
                            if (fieldDto.SortOrder.HasValue) existingField.SortOrder = fieldDto.SortOrder.Value;
                            existingField.DefaultValue = fieldDto.DefaultValue;
                        }
                        else
                        {
                            // Custom字段可以更新大部分属性
                            if (fieldDto.PropertyName != null) existingField.PropertyName = fieldDto.PropertyName;
                            if (fieldDto.DisplayName != null) existingField.DisplayName = fieldDto.DisplayName;
                            if (fieldDto.DataType != null) existingField.DataType = fieldDto.DataType;
                            existingField.Length = fieldDto.Length;
                            existingField.Precision = fieldDto.Precision;
                            existingField.Scale = fieldDto.Scale;
                            if (fieldDto.IsRequired.HasValue) existingField.IsRequired = fieldDto.IsRequired.Value;
                            if (fieldDto.IsEntityRef.HasValue) existingField.IsEntityRef = fieldDto.IsEntityRef.Value;
                            existingField.ReferencedEntityId = fieldDto.ReferencedEntityId;
                            if (fieldDto.LookupEntityName != null) existingField.LookupEntityName = fieldDto.LookupEntityName;
                            if (fieldDto.LookupDisplayField != null) existingField.LookupDisplayField = fieldDto.LookupDisplayField;
                            if (fieldDto.ForeignKeyAction.HasValue) existingField.ForeignKeyAction = fieldDto.ForeignKeyAction.Value;
                            if (fieldDto.SortOrder.HasValue) existingField.SortOrder = fieldDto.SortOrder.Value;
                            existingField.DefaultValue = fieldDto.DefaultValue;
                            existingField.ValidationRules = fieldDto.ValidationRules;
                        }

                        // 更新时间
                        existingField.UpdatedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        // 添加新字段（不使用前端生成的ID）
                        var newField = new FieldMetadata
                        {
                            // 不设置Id，让数据库自动生成
                            PropertyName = fieldDto.PropertyName ?? string.Empty,
                            DisplayName = fieldDto.DisplayName,
                            DataType = fieldDto.DataType ?? FieldDataType.String,
                            Length = fieldDto.Length,
                            Precision = fieldDto.Precision,
                            Scale = fieldDto.Scale,
                            IsRequired = fieldDto.IsRequired ?? false,
                            IsEntityRef = fieldDto.IsEntityRef ?? false,
                            ReferencedEntityId = fieldDto.ReferencedEntityId,
                            LookupEntityName = fieldDto.LookupEntityName,
                            LookupDisplayField = fieldDto.LookupDisplayField,
                            ForeignKeyAction = fieldDto.ForeignKeyAction ?? ForeignKeyAction.Restrict,
                            SortOrder = fieldDto.SortOrder ?? 0,
                            DefaultValue = fieldDto.DefaultValue,
                            ValidationRules = fieldDto.ValidationRules,
                            Source = FieldSource.Custom,  // 新增字段标记为Custom
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        definition.Fields.Add(newField);
                    }
                }
            }

            // 更新接口列表
            if (dto.Interfaces != null)
            {
                // 获取现有接口类型集合
                var existingInterfaces = definition.Interfaces.Select(i => i.InterfaceType).ToHashSet();
                var incomingInterfaces = dto.Interfaces.ToHashSet();

                // 删除不在新列表中的接口（已经在上面锁定检查中验证过）
                var interfacesToRemove = definition.Interfaces
                    .Where(i => !incomingInterfaces.Contains(i.InterfaceType))
                    .ToList();
                foreach (var iface in interfacesToRemove)
                {
                    definition.Interfaces.Remove(iface);
                }

                // 添加新接口
                var newInterfaceTypes = incomingInterfaces.Except(existingInterfaces).ToList();
                foreach (var interfaceType in newInterfaceTypes)
                {
                    var newInterface = new EntityInterface
                    {
                        InterfaceType = interfaceType,
                        IsEnabled = true
                    };
                    definition.Interfaces.Add(newInterface);

                    // 根据接口类型自动添加字段
                    var interfaceFields = InterfaceFieldMapping.GetFields(interfaceType);
                    foreach (var ifField in interfaceFields)
                    {
                        // 检查字段是否已存在
                        if (!definition.Fields.Any(f => f.PropertyName == ifField.PropertyName))
                        {
                            definition.Fields.Add(new FieldMetadata
                            {
                                PropertyName = ifField.PropertyName,
                                DisplayName = null,  // 接口字段暂不提供默认多语言
                                DataType = ifField.DataType,
                                Length = ifField.Length,
                                IsRequired = ifField.IsRequired,
                                IsEntityRef = ifField.IsEntityRef,
                                TableName = ifField.ReferenceTable,
                                DefaultValue = ifField.DefaultValue,
                                SortOrder = 0,
                                Source = FieldSource.Interface
                            });
                        }
                    }
                }
            }

            await db.SaveChangesAsync();

            logger.LogInformation("[EntityDefinition] Entity updated successfully: {Id}", id);

            // Construct response DTO
            var resultDto = new EntityDefinitionDto
            {
                Id = definition.Id,
                Namespace = definition.Namespace,
                EntityName = definition.EntityName,
                FullTypeName = definition.FullTypeName,
                EntityRoute = definition.EntityRoute,
                DisplayNameTranslations = definition.DisplayName != null
                    ? new MultilingualText(definition.DisplayName)
                    : new MultilingualText(),
                DescriptionTranslations = definition.Description != null
                    ? new MultilingualText(definition.Description)
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
                UpdatedBy = definition.UpdatedBy
            };

            return Results.Ok(new SuccessResponse<EntityDefinitionDto>(resultDto));
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
