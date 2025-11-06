using BobCrm.Api.Services;

namespace BobCrm.Api.Endpoints;

public static class EntityMetadataEndpoints
{
    public static void MapEntityMetadataEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/entities");

        // 获取所有可用的根实体（用于模板创建）
        group.MapGet("/", async (EntityMetadataService entityService) =>
        {
            var entities = await entityService.GetAvailableRootEntitiesAsync();
            
            Console.WriteLine($"[EntityMetadataEndpoints] GET /api/entities - Found {entities.Count} entities");
            foreach (var e in entities)
            {
                Console.WriteLine($"  - {e.EntityName} (route={e.EntityRoute}, displayKey={e.DisplayNameKey})");
            }
            
            var result = entities.Select(e => new
            {
                entityType = e.EntityRoute,      // 前端使用EntityRoute（customer）
                entityName = e.EntityName,       // Customer
                displayNameKey = e.DisplayNameKey,
                descriptionKey = e.DescriptionKey,
                apiEndpoint = e.ApiEndpoint,
                icon = e.Icon,
                category = e.Category,
                order = e.Order
            }).ToList();
            
            Console.WriteLine($"[EntityMetadataEndpoints] Returning {result.Count} items");
            return Results.Ok(result);
        })
        .WithName("GetAvailableEntities")
        .WithTags("Entities")
        .WithOpenApi();

        // 获取所有根实体（包括未启用的，仅管理员）
        group.MapGet("/all", async (EntityMetadataService entityService) =>
        {
            var entities = await entityService.GetAllRootEntitiesAsync();
            
            return Results.Ok(entities.Select(e => new
            {
                e.EntityType,
                e.EntityName,
                e.EntityRoute,
                e.DisplayNameKey,
                e.DescriptionKey,
                e.ApiEndpoint,
                e.IsEnabled,
                e.IsRootEntity,
                e.Icon,
                e.Category,
                e.Order
            }));
        })
        .RequireAuthorization() // 需要管理员权限
        .WithName("GetAllEntities")
        .WithTags("Entities")
        .WithOpenApi();

        // 验证实体路由是否有效
        group.MapGet("/{entityRoute}/validate", async (string entityRoute, EntityMetadataService entityService) =>
        {
            var isValid = await entityService.IsValidEntityRouteAsync(entityRoute);
            var entity = await entityService.GetEntityMetadataByRouteAsync(entityRoute);
            
            return Results.Ok(new
            {
                isValid,
                entityRoute,
                entity = entity != null ? new
                {
                    entity.EntityName,
                    entity.DisplayNameKey,
                    entity.ApiEndpoint,
                    entity.IsEnabled
                } : null
            });
        })
        .WithName("ValidateEntityRoute")
        .WithTags("Entities")
        .WithOpenApi();
    }
}

