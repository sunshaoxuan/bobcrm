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
            
            return Results.Ok(entities.Select(e => new
            {
                e.EntityType,
                e.DisplayNameKey,
                e.DescriptionKey,
                e.ApiEndpoint,
                e.Icon,
                e.Category,
                e.Order
            }));
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
                e.DisplayNameKey,
                e.DescriptionKey,
                e.ApiEndpoint,
                e.IsEnabled,
                e.Icon,
                e.Category,
                e.Order
            }));
        })
        .RequireAuthorization() // 需要管理员权限
        .WithName("GetAllEntities")
        .WithTags("Entities")
        .WithOpenApi();

        // 验证实体类型是否有效
        group.MapGet("/{entityType}/validate", async (string entityType, EntityMetadataService entityService) =>
        {
            var isValid = await entityService.IsValidEntityTypeAsync(entityType);
            var entity = await entityService.GetEntityMetadataAsync(entityType);
            
            return Results.Ok(new
            {
                isValid,
                entityType,
                entity = entity != null ? new
                {
                    entity.DisplayNameKey,
                    entity.ApiEndpoint,
                    entity.IsEnabled
                } : null
            });
        })
        .WithName("ValidateEntityType")
        .WithTags("Entities")
        .WithOpenApi();
    }
}

