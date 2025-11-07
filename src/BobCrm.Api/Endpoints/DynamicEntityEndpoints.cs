using System.Security.Claims;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
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
            [FromBody] QueryRequest request,
            ReflectionPersistenceService persistenceService,
            ILogger<Program> logger) =>
        {
            try
            {
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

                return Results.Ok(new
                {
                    data = results,
                    total = count,
                    page = request.Skip.HasValue && request.Take.HasValue
                        ? (request.Skip.Value / request.Take.Value) + 1
                        : 1,
                    pageSize = request.Take ?? 100
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[DynamicEntity] Query failed: {Message}", ex.Message);
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("QueryDynamicEntities")
        .WithSummary("查询动态实体列表")
        .WithDescription("支持过滤、排序、分页");

        // 根据ID查询单个实体
        group.MapGet("/{fullTypeName}/{id:int}", async (
            string fullTypeName,
            int id,
            ReflectionPersistenceService persistenceService,
            ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation("[DynamicEntity] Getting {EntityType} with ID {Id}", fullTypeName, id);

                var entity = await persistenceService.GetByIdAsync(fullTypeName, id);

                if (entity == null)
                    return Results.NotFound(new { error = $"Entity with ID {id} not found" });

                return Results.Ok(entity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[DynamicEntity] Get failed: {Message}", ex.Message);
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("GetDynamicEntityById")
        .WithSummary("根据ID查询动态实体")
        .WithDescription("返回单个实体对象");

        // 原始SQL查询（表名）
        group.MapPost("/raw/{tableName}/query", async (
            string tableName,
            [FromBody] QueryRequest request,
            ReflectionPersistenceService persistenceService,
            ILogger<Program> logger) =>
        {
            try
            {
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

                return Results.Ok(new
                {
                    data = results,
                    count = results.Count
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[DynamicEntity] Raw query failed: {Message}", ex.Message);
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("RawQueryDynamicEntities")
        .WithSummary("原始SQL查询")
        .WithDescription("直接使用表名查询（不需要加载实体类型）");

        // ==================== 创建 ====================

        // 创建实体
        group.MapPost("/{fullTypeName}", async (
            string fullTypeName,
            [FromBody] Dictionary<string, object> data,
            ReflectionPersistenceService persistenceService,
            HttpContext http,
            ILogger<Program> logger) =>
        {
            try
            {
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

                return Results.Created($"/api/dynamic-entities/{fullTypeName}/{GetEntityId(entity)}", entity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[DynamicEntity] Create failed: {Message}", ex.Message);
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("CreateDynamicEntity")
        .WithSummary("创建动态实体")
        .WithDescription("创建新的实体记录");

        // ==================== 更新 ====================

        // 更新实体
        group.MapPut("/{fullTypeName}/{id:int}", async (
            string fullTypeName,
            int id,
            [FromBody] Dictionary<string, object> data,
            ReflectionPersistenceService persistenceService,
            HttpContext http,
            ILogger<Program> logger) =>
        {
            try
            {
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
                    return Results.NotFound(new { error = $"Entity with ID {id} not found" });

                logger.LogInformation("[DynamicEntity] Updated {EntityType} successfully", fullTypeName);

                return Results.Ok(entity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[DynamicEntity] Update failed: {Message}", ex.Message);
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("UpdateDynamicEntity")
        .WithSummary("更新动态实体")
        .WithDescription("更新现有实体记录");

        // ==================== 删除 ====================

        // 删除实体
        group.MapDelete("/{fullTypeName}/{id:int}", async (
            string fullTypeName,
            int id,
            ReflectionPersistenceService persistenceService,
            ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation("[DynamicEntity] Deleting {EntityType} with ID {Id}", fullTypeName, id);

                var deleted = await persistenceService.DeleteAsync(fullTypeName, id);

                if (!deleted)
                    return Results.NotFound(new { error = $"Entity with ID {id} not found" });

                logger.LogInformation("[DynamicEntity] Deleted {EntityType} successfully", fullTypeName);

                return Results.NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[DynamicEntity] Delete failed: {Message}", ex.Message);
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("DeleteDynamicEntity")
        .WithSummary("删除动态实体")
        .WithDescription("删除实体记录");

        // ==================== 统计 ====================

        // 统计记录数
        group.MapPost("/{fullTypeName}/count", async (
            string fullTypeName,
            [FromBody] CountRequest request,
            ReflectionPersistenceService persistenceService,
            ILogger<Program> logger) =>
        {
            try
            {
                logger.LogInformation("[DynamicEntity] Counting {EntityType}", fullTypeName);

                var count = await persistenceService.CountAsync(fullTypeName, request.Filters);

                return Results.Ok(new { count });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[DynamicEntity] Count failed: {Message}", ex.Message);
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("CountDynamicEntities")
        .WithSummary("统计动态实体数量")
        .WithDescription("统计符合条件的记录数");

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

/// <summary>
/// 查询请求DTO
/// </summary>
public record QueryRequest
{
    public List<FilterCondition>? Filters { get; init; }
    public string? OrderBy { get; init; }
    public bool OrderByDescending { get; init; }
    public int? Skip { get; init; }
    public int? Take { get; init; }
}

/// <summary>
/// 统计请求DTO
/// </summary>
public record CountRequest
{
    public List<FilterCondition>? Filters { get; init; }
}
