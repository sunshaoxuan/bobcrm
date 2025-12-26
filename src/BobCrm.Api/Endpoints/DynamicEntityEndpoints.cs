using System.Security.Claims;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.Responses.DynamicEntity;
using BobCrm.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// 动态实体CRUD端点
/// 提供运行时加载的自定义实体的数据操作
/// </summary>
public static class DynamicEntityEndpoints
{
    public static IEndpointRouteBuilder MapDynamicEntityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dynamic-entities")
            .WithTags("动态实体数据操作")
            .WithOpenApi()
            .RequireAuthorization();

        // ==================== 查询 ====================

        // 查询实体列表
        group.MapPost("/{fullTypeName}/query", async (
            string fullTypeName,
            [FromQuery] string? lang,
            [FromBody] QueryRequest request,
            IReflectionPersistenceService persistenceService,
            IFieldMetadataCache fieldMetadataCache,
            ILocalization loc,
            HttpContext http,
            CancellationToken ct,
            ILogger<Program> logger) =>
        {
            var uiLang = LangHelper.GetLang(http);
            var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
            logger.LogInformation("[DynamicEntity] Querying {EntityType}", fullTypeName);

            var options = new QueryOptions
            {
                Filters = request.Filters,
                OrderBy = request.OrderBy,
                OrderByDescending = request.OrderByDescending,
                Skip = request.Skip,
                Take = request.Take ?? 100 // 默认100条
            };

            var results = await persistenceService.QueryAsync(fullTypeName, options);
            var count = await persistenceService.CountAsync(fullTypeName, request.Filters);

            var fields = await fieldMetadataCache.GetFieldsAsync(fullTypeName, loc, targetLang, ct);

            var dto = new DynamicEntityQueryResultDto
            {
                Meta = new DynamicEntityMetaDto
                {
                    Fields = fields
                },
                Data = results,
                Total = count,
                Page = request.Skip.HasValue && request.Take.HasValue
                    ? (request.Skip.Value / request.Take.Value) + 1
                    : 1,
                PageSize = request.Take ?? 100
            };

            return Results.Ok(new SuccessResponse<DynamicEntityQueryResultDto>(dto));
        })
        .WithName("QueryDynamicEntities")
        .WithSummary("查询动态实体列表")
        .WithDescription("支持过滤、排序、分页")
        .Produces<SuccessResponse<DynamicEntityQueryResultDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        // 根据ID查询单个实体
        group.MapGet("/{fullTypeName}/{id:int}", async (
            string fullTypeName,
            int id,
            [FromQuery] string? lang,
            [FromQuery] bool? includeMeta,
            IReflectionPersistenceService persistenceService,
            IFieldMetadataCache fieldMetadataCache,
            ILocalization loc,
            HttpContext http,
            CancellationToken ct,
            ILogger<Program> logger) =>
        {
            var uiLang = LangHelper.GetLang(http);
            var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
            logger.LogInformation("[DynamicEntity] Getting {EntityType} with ID {Id}", fullTypeName, id);

            var entity = await persistenceService.GetByIdAsync(fullTypeName, id);

            if (entity == null)
                return Results.NotFound(new ErrorResponse(
                    string.Format(loc.T("ERR_DYNAMIC_ENTITY_NOT_FOUND", uiLang), id),
                    "DYNAMIC_ENTITY_NOT_FOUND"));

            var result = new DynamicEntityGetResultDto
            {
                Meta = includeMeta == true
                    ? new DynamicEntityMetaDto { Fields = await fieldMetadataCache.GetFieldsAsync(fullTypeName, loc, targetLang, ct) }
                    : null,
                Data = entity
            };

            return Results.Ok(new SuccessResponse<DynamicEntityGetResultDto>(result));
        })
        .WithName("GetDynamicEntityById")
        .WithSummary("根据ID查询动态实体")
        .WithDescription("返回单个实体对象；可选 includeMeta=true 返回元数据")
        .Produces<SuccessResponse<DynamicEntityGetResultDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // 原始SQL查询（表名）
        group.MapPost("/raw/{tableName}/query", async (
            string tableName,
            [FromBody] QueryRequest request,
            IReflectionPersistenceService persistenceService,
            ILocalization loc,
            HttpContext http,
            ILogger<Program> logger) =>
        {
            var lang = LangHelper.GetLang(http);
            logger.LogInformation("[DynamicEntity] Raw query on table {TableName}", tableName);

            var options = new QueryOptions
            {
                Filters = request.Filters,
                OrderBy = request.OrderBy,
                OrderByDescending = request.OrderByDescending,
                Skip = request.Skip,
                Take = request.Take ?? 100
            };

            var results = await persistenceService.QueryRawAsync(tableName, options);

            var dto = new DynamicEntityRawQueryResultDto
            {
                Data = results,
                Count = results.Count
            };

            return Results.Ok(new SuccessResponse<DynamicEntityRawQueryResultDto>(dto));
        })
        .WithName("RawQueryDynamicEntities")
        .WithSummary("原始SQL查询")
        .WithDescription("直接使用表名查询（不需要加载实体类型）")
        .Produces<SuccessResponse<DynamicEntityRawQueryResultDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        // ==================== 创建 ====================

        // 创建实体
        group.MapPost("/{fullTypeName}", async (
            string fullTypeName,
            [FromBody] Dictionary<string, object> data,
            IReflectionPersistenceService persistenceService,
            HttpContext http,
            ILocalization loc,
            ILogger<Program> logger) =>
        {
            var lang = LangHelper.GetLang(http);
            var uid = http.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            logger.LogInformation("[DynamicEntity] Creating {EntityType}", fullTypeName);

            // 自动添加审计字段（如果存在）
            if (data.ContainsKey("CreatedBy") || data.ContainsKey("createdBy"))
            {
                data["CreatedBy"] = uid;
            }
            if (data.ContainsKey("CreatedAt") || data.ContainsKey("createdAt"))
            {
                data["CreatedAt"] = DateTime.UtcNow;
            }

            var entity = await persistenceService.CreateAsync(fullTypeName, data);

            logger.LogInformation("[DynamicEntity] Created {EntityType} successfully", fullTypeName);

            var result = new DynamicEntityGetResultDto { Data = entity };
            return Results.Created($"/api/dynamic-entities/{fullTypeName}/{GetEntityId(entity)}", new SuccessResponse<DynamicEntityGetResultDto>(result));
        })
        .WithName("CreateDynamicEntity")
        .WithSummary("创建动态实体")
        .WithDescription("创建新的实体记录")
        .Produces<SuccessResponse<DynamicEntityGetResultDto>>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        // ==================== 更新 ====================

        // 更新实体
        group.MapPut("/{fullTypeName}/{id:int}", async (
            string fullTypeName,
            int id,
            [FromBody] Dictionary<string, object> data,
            IReflectionPersistenceService persistenceService,
            HttpContext http,
            ILocalization loc,
            ILogger<Program> logger) =>
        {
            var lang = LangHelper.GetLang(http);
            var uid = http.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            logger.LogInformation("[DynamicEntity] Updating {EntityType} with ID {Id}", fullTypeName, id);

            // 自动添加审计字段（如果存在）
            if (data.ContainsKey("UpdatedBy") || data.ContainsKey("updatedBy"))
            {
                data["UpdatedBy"] = uid;
            }
            if (data.ContainsKey("UpdatedAt") || data.ContainsKey("updatedAt"))
            {
                data["UpdatedAt"] = DateTime.UtcNow;
            }

            var entity = await persistenceService.UpdateAsync(fullTypeName, id, data);

            if (entity == null)
                return Results.NotFound(new ErrorResponse(
                    string.Format(loc.T("ERR_DYNAMIC_ENTITY_NOT_FOUND", lang), id),
                    "DYNAMIC_ENTITY_NOT_FOUND"));

            logger.LogInformation("[DynamicEntity] Updated {EntityType} successfully", fullTypeName);

            var result = new DynamicEntityGetResultDto { Data = entity };
            return Results.Ok(new SuccessResponse<DynamicEntityGetResultDto>(result));
        })
        .WithName("UpdateDynamicEntity")
        .WithSummary("更新动态实体")
        .WithDescription("更新现有实体记录")
        .Produces<SuccessResponse<DynamicEntityGetResultDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // ==================== 删除 ====================

        // 删除实体
        group.MapDelete("/{fullTypeName}/{id:int}", async (
            string fullTypeName,
            int id,
            IReflectionPersistenceService persistenceService,
            ILocalization loc,
            HttpContext http,
            ILogger<Program> logger) =>
        {
            var lang = LangHelper.GetLang(http);
            logger.LogInformation("[DynamicEntity] Deleting {EntityType} with ID {Id}", fullTypeName, id);

            var deleted = await persistenceService.DeleteAsync(fullTypeName, id);

            if (!deleted)
                return Results.NotFound(new ErrorResponse(
                    string.Format(loc.T("ERR_DYNAMIC_ENTITY_NOT_FOUND", lang), id),
                    "DYNAMIC_ENTITY_NOT_FOUND"));

            logger.LogInformation("[DynamicEntity] Deleted {EntityType} successfully", fullTypeName);

            return Results.Ok(new SuccessResponse());
        })
        .WithName("DeleteDynamicEntity")
        .WithSummary("删除动态实体")
        .WithDescription("删除实体记录")
        .Produces<SuccessResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // ==================== 统计 ====================

        // 统计记录数
        group.MapPost("/{fullTypeName}/count", async (
            string fullTypeName,
            [FromBody] CountRequest request,
            IReflectionPersistenceService persistenceService,
            ILocalization loc,
            HttpContext http,
            ILogger<Program> logger) =>
        {
            var lang = LangHelper.GetLang(http);
            logger.LogInformation("[DynamicEntity] Counting {EntityType}", fullTypeName);

            var count = await persistenceService.CountAsync(fullTypeName, request.Filters);

            var dto = new EntityCountResponse { Count = count };
            return Results.Ok(new SuccessResponse<EntityCountResponse>(dto));
        })
        .WithName("CountDynamicEntities")
        .WithSummary("统计动态实体数量")
        .WithDescription("统计符合条件的记录数")
        .Produces<SuccessResponse<EntityCountResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        return app;
    }

    /// <summary>
    /// 获取实体的ID（反射）
    /// </summary>
    private static object? GetEntityId(object entity)
    {
        var idProperty = entity.GetType().GetProperty("Id");
        return idProperty?.GetValue(entity);
    }
}
