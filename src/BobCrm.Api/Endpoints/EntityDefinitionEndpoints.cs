using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Domain.Models;
using BobCrm.Api.Core.DomainCommon;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// 实体定义管理端点 - 支持实体自定义与发布
/// </summary>
public static class EntityDefinitionEndpoints
{
    public static IEndpointRouteBuilder MapEntityDefinitionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/entity-definitions")
            .WithTags("实体定义管理")
            .WithOpenApi()
            .RequireAuthorization();

        // ==================== 查询 ====================

        // 获取所有实体定义列表
        group.MapGet("", async (AppDbContext db, HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            var definitions = await db.EntityDefinitions
                .Include(ed => ed.Fields)
                .Include(ed => ed.Interfaces)
                .OrderByDescending(ed => ed.UpdatedAt)
                .Select(ed => new
                {
                    ed.Id,
                    ed.Namespace,
                    ed.EntityName,
                    ed.DisplayNameKey,
                    ed.DescriptionKey,
                    ed.StructureType,
                    ed.Status,
                    ed.IsLocked,
                    ed.CreatedAt,
                    ed.UpdatedAt,
                    ed.CreatedBy,
                    FieldCount = ed.Fields.Count,
                    Interfaces = ed.Interfaces.Select(i => i.InterfaceType).ToList()
                })
                .ToListAsync();

            return Results.Json(definitions);
        })
        .WithName("GetEntityDefinitions")
        .WithSummary("获取实体定义列表")
        .WithDescription("获取所有实体定义的列表，包括字段数量和接口类型");

        // 获取单个实体定义详情
        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var definition = await db.EntityDefinitions
                .Include(ed => ed.Fields.OrderBy(f => f.SortOrder))
                .Include(ed => ed.Interfaces)
                .FirstOrDefaultAsync(ed => ed.Id == id);

            if (definition == null)
                return Results.NotFound(new { error = "实体定义不存在" });

            return Results.Json(definition);
        })
        .WithName("GetEntityDefinition")
        .WithSummary("获取实体定义详情")
        .WithDescription("获取指定实体定义的完整信息，包括所有字段和接口");

        // 检查实体是否被引用
        group.MapGet("/{id:guid}/referenced", async (Guid id, AppDbContext db) =>
        {
            var definition = await db.EntityDefinitions.FindAsync(id);
            if (definition == null)
                return Results.NotFound(new { error = "实体定义不存在" });

            // 检查是否被FormTemplate引用
            var templateCount = await db.FormTemplates
                .Where(t => t.EntityType == definition.FullName)
                .CountAsync();

            return Results.Json(new
            {
                isReferenced = templateCount > 0,
                referenceCount = templateCount,
                referencedBy = new
                {
                    formTemplates = templateCount
                }
            });
        })
        .WithName("CheckEntityReferenced")
        .WithSummary("检查实体是否被引用")
        .WithDescription("检查实体定义是否被模板或其他地方引用");

        // ==================== 创建 ====================

        // 创建新实体定义
        group.MapPost("", async (
            CreateEntityDefinitionDto dto,
            AppDbContext db,
            HttpContext http,
            ILogger<Program> logger) =>
        {
            var uid = http.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            logger.LogInformation("[EntityDefinition] Creating new entity: {Namespace}.{EntityName}",
                dto.Namespace, dto.EntityName);

            // 验证必填字段
            if (string.IsNullOrWhiteSpace(dto.Namespace))
                return Results.BadRequest(new { error = "命名空间不能为空" });
            if (string.IsNullOrWhiteSpace(dto.EntityName))
                return Results.BadRequest(new { error = "实体名不能为空" });
            if (string.IsNullOrWhiteSpace(dto.DisplayNameKey))
                return Results.BadRequest(new { error = "显示名不能为空" });

            // 检查是否已存在同名实体
            var exists = await db.EntityDefinitions
                .AnyAsync(ed => ed.Namespace == dto.Namespace && ed.EntityName == dto.EntityName);
            if (exists)
            {
                logger.LogWarning("[EntityDefinition] Entity already exists: {Namespace}.{EntityName}",
                    dto.Namespace, dto.EntityName);
                return Results.Conflict(new { error = "实体已存在" });
            }

            // 创建实体定义
            var definition = new EntityDefinition
            {
                Namespace = dto.Namespace,
                EntityName = dto.EntityName,
                DisplayNameKey = dto.DisplayNameKey,
                DescriptionKey = dto.DescriptionKey,
                StructureType = dto.StructureType ?? EntityStructureType.Single,
                Status = EntityStatus.Draft,
                CreatedBy = uid,
                UpdatedBy = uid
            };

            // 添加字段
            if (dto.Fields != null)
            {
                foreach (var fieldDto in dto.Fields)
                {
                    definition.Fields.Add(new FieldMetadata
                    {
                        PropertyName = fieldDto.PropertyName,
                        DisplayNameKey = fieldDto.DisplayNameKey,
                        DataType = fieldDto.DataType,
                        Length = fieldDto.Length,
                        Precision = fieldDto.Precision,
                        Scale = fieldDto.Scale,
                        IsRequired = fieldDto.IsRequired,
                        IsEntityRef = fieldDto.IsEntityRef,
                        ReferencedEntityId = fieldDto.ReferencedEntityId,
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
                                DisplayNameKey = $"FIELD_{ifField.PropertyName.ToUpper()}",
                                DataType = ifField.DataType,
                                Length = ifField.Length,
                                IsRequired = ifField.IsRequired,
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

            return Results.Created($"/api/entity-definitions/{definition.Id}", definition);
        })
        .WithName("CreateEntityDefinition")
        .WithSummary("创建实体定义")
        .WithDescription("创建新的实体定义，可包含字段和接口");

        // ==================== 更新 ====================

        // 更新实体定义
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateEntityDefinitionDto dto,
            AppDbContext db,
            HttpContext http,
            ILogger<Program> logger) =>
        {
            var uid = http.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var definition = await db.EntityDefinitions
                .Include(ed => ed.Fields)
                .Include(ed => ed.Interfaces)
                .FirstOrDefaultAsync(ed => ed.Id == id);

            if (definition == null)
                return Results.NotFound(new { error = "实体定义不存在" });

            logger.LogInformation("[EntityDefinition] Updating entity: {Id}, IsLocked={IsLocked}",
                id, definition.IsLocked);

            // 如果实体已被引用锁定，则进行严格校验
            if (definition.IsLocked)
            {
                // 不允许修改命名空间和实体名
                if (dto.Namespace != null && dto.Namespace != definition.Namespace)
                    return Results.BadRequest(new { error = "实体已被引用，不能修改命名空间" });
                if (dto.EntityName != null && dto.EntityName != definition.EntityName)
                    return Results.BadRequest(new { error = "实体已被引用，不能修改实体名" });

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
                        return Results.BadRequest(new { error = "实体已被引用，不能删除字段" });
                    }

                    // 检查字段类型修改和长度缩小
                    foreach (var fieldDto in dto.Fields.Where(f => f.Id.HasValue))
                    {
                        var existingField = definition.Fields.FirstOrDefault(f => f.Id == fieldDto.Id!.Value);
                        if (existingField != null)
                        {
                            if (fieldDto.DataType != null && fieldDto.DataType != existingField.DataType)
                                return Results.BadRequest(new { error = $"字段 {existingField.PropertyName} 已被引用，不能修改数据类型" });

                            if (fieldDto.Length.HasValue && fieldDto.Length < existingField.Length)
                                return Results.BadRequest(new { error = $"字段 {existingField.PropertyName} 的长度只能增大，不能缩小" });
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
                        return Results.BadRequest(new { error = "实体已被引用，不能删除已锁定的接口" });
                    }
                }
            }

            // 更新基本信息
            if (dto.Namespace != null) definition.Namespace = dto.Namespace;
            if (dto.EntityName != null) definition.EntityName = dto.EntityName;
            if (dto.DisplayNameKey != null) definition.DisplayNameKey = dto.DisplayNameKey;
            if (dto.DescriptionKey != null) definition.DescriptionKey = dto.DescriptionKey;
            if (dto.StructureType != null) definition.StructureType = dto.StructureType;

            definition.UpdatedAt = DateTime.UtcNow;
            definition.UpdatedBy = uid;

            // 如果状态是Published，改为Modified
            if (definition.Status == EntityStatus.Published)
                definition.Status = EntityStatus.Modified;

            // 更新字段（简化版，实际可能需要更复杂的合并逻辑）
            // 这里仅作为示例，实际应该根据Id进行更新/删除/新增

            await db.SaveChangesAsync();

            logger.LogInformation("[EntityDefinition] Entity updated successfully: {Id}", id);

            return Results.Ok(definition);
        })
        .WithName("UpdateEntityDefinition")
        .WithSummary("更新实体定义")
        .WithDescription("更新实体定义，如果实体已被引用则受到限制");

        // ==================== 删除 ====================

        // 删除实体定义
        group.MapDelete("/{id:guid}", async (
            Guid id,
            AppDbContext db,
            ILogger<Program> logger) =>
        {
            var definition = await db.EntityDefinitions
                .Include(ed => ed.Fields)
                .Include(ed => ed.Interfaces)
                .FirstOrDefaultAsync(ed => ed.Id == id);

            if (definition == null)
                return Results.NotFound(new { error = "实体定义不存在" });

            // 检查是否已发布
            if (definition.Status == EntityStatus.Published)
            {
                logger.LogWarning("[EntityDefinition] Cannot delete published entity: {Id}", id);
                return Results.BadRequest(new { error = "已发布的实体不能直接删除，请先撤销发布" });
            }

            // 检查是否被引用
            var templateCount = await db.FormTemplates
                .Where(t => t.EntityType == definition.FullName)
                .CountAsync();

            if (templateCount > 0)
            {
                logger.LogWarning("[EntityDefinition] Cannot delete referenced entity: {Id}, TemplateCount={Count}",
                    id, templateCount);
                return Results.BadRequest(new { error = "实体已被模板引用，不能删除" });
            }

            db.EntityDefinitions.Remove(definition);
            await db.SaveChangesAsync();

            logger.LogInformation("[EntityDefinition] Entity deleted successfully: {Id}", id);

            return Results.NoContent();
        })
        .WithName("DeleteEntityDefinition")
        .WithSummary("删除实体定义")
        .WithDescription("删除实体定义（仅限草稿状态且未被引用）");

        // ==================== 发布 ====================

        // 发布新实体（CREATE TABLE）
        group.MapPost("/{id:guid}/publish", async (
            Guid id,
            AppDbContext db,
            Services.EntityPublishingService publishService,
            HttpContext http,
            ILogger<Program> logger) =>
        {
            var uid = http.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            logger.LogInformation("[Publish] Publishing new entity: {Id}", id);

            var result = await publishService.PublishNewEntityAsync(id, uid);

            if (!result.Success)
            {
                logger.LogError("[Publish] Failed: {Error}", result.ErrorMessage);
                return Results.BadRequest(new { error = result.ErrorMessage });
            }

            return Results.Ok(new
            {
                success = true,
                scriptId = result.ScriptId,
                ddlScript = result.DDLScript,
                message = "实体发布成功"
            });
        })
        .WithName("PublishEntity")
        .WithSummary("发布新实体")
        .WithDescription("发布新实体定义，生成并执行CREATE TABLE语句");

        // 发布实体修改（ALTER TABLE）
        group.MapPost("/{id:guid}/publish-changes", async (
            Guid id,
            AppDbContext db,
            Services.EntityPublishingService publishService,
            HttpContext http,
            ILogger<Program> logger) =>
        {
            var uid = http.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            logger.LogInformation("[Publish] Publishing changes for entity: {Id}", id);

            var result = await publishService.PublishEntityChangesAsync(id, uid);

            if (!result.Success)
            {
                logger.LogError("[Publish] Failed: {Error}", result.ErrorMessage);
                return Results.BadRequest(new { error = result.ErrorMessage });
            }

            return Results.Ok(new
            {
                success = true,
                scriptId = result.ScriptId,
                ddlScript = result.DDLScript,
                changeAnalysis = new
                {
                    newFieldsCount = result.ChangeAnalysis?.NewFields.Count ?? 0,
                    lengthIncreasesCount = result.ChangeAnalysis?.LengthIncreases.Count ?? 0,
                    hasDestructiveChanges = result.ChangeAnalysis?.HasDestructiveChanges ?? false
                },
                message = "实体修改发布成功"
            });
        })
        .WithName("PublishEntityChanges")
        .WithSummary("发布实体修改")
        .WithDescription("发布实体定义的修改，生成并执行ALTER TABLE语句");

        // 预览DDL脚本（不执行）
        group.MapGet("/{id:guid}/preview-ddl", async (
            Guid id,
            AppDbContext db,
            Services.PostgreSQLDDLGenerator ddlGenerator) =>
        {
            var definition = await db.EntityDefinitions
                .Include(ed => ed.Fields.OrderBy(f => f.SortOrder))
                .Include(ed => ed.Interfaces)
                .FirstOrDefaultAsync(ed => ed.Id == id);

            if (definition == null)
                return Results.NotFound(new { error = "实体定义不存在" });

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
                    ddlScript = "-- 无变更";
                }
            }
            else
            {
                ddlScript = "-- 实体已发布，无待发布的变更";
            }

            return Results.Ok(new
            {
                entityId = id,
                entityName = definition.EntityName,
                status = definition.Status,
                ddlScript
            });
        })
        .WithName("PreviewDDL")
        .WithSummary("预览DDL脚本")
        .WithDescription("预览将要执行的DDL脚本（不实际执行）");

        // 获取DDL执行历史
        group.MapGet("/{id:guid}/ddl-history", async (
            Guid id,
            AppDbContext db,
            Services.DDLExecutionService ddlExecutor) =>
        {
            var history = await ddlExecutor.GetDDLHistoryAsync(id);

            return Results.Ok(history.Select(h => new
            {
                h.Id,
                h.ScriptType,
                h.Status,
                h.CreatedAt,
                h.ExecutedAt,
                h.CreatedBy,
                h.ErrorMessage,
                scriptPreview = h.SqlScript.Length > 200 ? h.SqlScript.Substring(0, 200) + "..." : h.SqlScript
            }));
        })
        .WithName("GetDDLHistory")
        .WithSummary("获取DDL执行历史")
        .WithDescription("获取实体定义的所有DDL脚本执行历史");

        return app;
    }
}

// ==================== DTOs ====================

/// <summary>
/// 创建实体定义DTO
/// </summary>
public record CreateEntityDefinitionDto
{
    public string Namespace { get; init; } = "BobCrm.Domain.Custom";
    public string EntityName { get; init; } = string.Empty;
    public string DisplayNameKey { get; init; } = string.Empty;
    public string? DescriptionKey { get; init; }
    public string? StructureType { get; init; }
    public List<CreateFieldMetadataDto>? Fields { get; init; }
    public List<string>? Interfaces { get; init; }
}

/// <summary>
/// 创建字段元数据DTO
/// </summary>
public record CreateFieldMetadataDto
{
    public string PropertyName { get; init; } = string.Empty;
    public string DisplayNameKey { get; init; } = string.Empty;
    public string DataType { get; init; } = FieldDataType.String;
    public int? Length { get; init; }
    public int? Precision { get; init; }
    public int? Scale { get; init; }
    public bool IsRequired { get; init; }
    public bool IsEntityRef { get; init; }
    public Guid? ReferencedEntityId { get; init; }
    public int SortOrder { get; init; }
    public string? DefaultValue { get; init; }
    public string? ValidationRules { get; init; }
}

/// <summary>
/// 更新实体定义DTO
/// </summary>
public record UpdateEntityDefinitionDto
{
    public string? Namespace { get; init; }
    public string? EntityName { get; init; }
    public string? DisplayNameKey { get; init; }
    public string? DescriptionKey { get; init; }
    public string? StructureType { get; init; }
    public List<UpdateFieldMetadataDto>? Fields { get; init; }
    public List<string>? Interfaces { get; init; }
}

/// <summary>
/// 更新字段元数据DTO
/// </summary>
public record UpdateFieldMetadataDto
{
    public Guid? Id { get; init; }
    public string? PropertyName { get; init; }
    public string? DisplayNameKey { get; init; }
    public string? DataType { get; init; }
    public int? Length { get; init; }
    public int? Precision { get; init; }
    public int? Scale { get; init; }
    public bool? IsRequired { get; init; }
    public bool? IsEntityRef { get; init; }
    public Guid? ReferencedEntityId { get; init; }
    public int? SortOrder { get; init; }
    public string? DefaultValue { get; init; }
    public string? ValidationRules { get; init; }
}
