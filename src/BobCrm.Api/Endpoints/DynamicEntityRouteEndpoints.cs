using System.Security.Claims;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// 动态实体“短路由”端点：/api/{entityType}s/{id}
/// 用于运行时 PageLoader 与模板生成中的 apiEndpoint。
/// </summary>
public static class DynamicEntityRouteEndpoints
{
    public static IEndpointRouteBuilder MapDynamicEntityRouteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api")
            .WithTags("动态实体（短路由）")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/{entityPlural}/{id:int}", async (
            string entityPlural,
            int id,
            AppDbContext db,
            DynamicEntityService dynamicEntityService,
            IReflectionPersistenceService persistenceService,
            HttpContext http,
            CancellationToken ct) =>
        {
            var entityType = NormalizeEntityRoute(entityPlural);
            var definition = await ResolveEntityDefinitionAsync(db, entityType, ct);
            if (definition == null)
            {
                return Results.NotFound();
            }

            await EnsureCompiledAsync(dynamicEntityService, definition, ct);

            var entity = await persistenceService.GetByIdAsync(definition.FullTypeName, id);
            if (entity == null)
            {
                return Results.NotFound();
            }

            var dict = ToDictionary(entity);
            return Results.Ok(ToPageLoaderResponse(dict));
        })
        .WithName("GetDynamicEntityByShortRoute")
        .WithSummary("按短路由获取动态实体")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapPut("/{entityPlural}/{id:int}", async (
            string entityPlural,
            int id,
            DynamicEntityUpdateRequest payload,
            AppDbContext db,
            DynamicEntityService dynamicEntityService,
            IReflectionPersistenceService persistenceService,
            HttpContext http,
            CancellationToken ct) =>
        {
            var entityType = NormalizeEntityRoute(entityPlural);
            var definition = await ResolveEntityDefinitionAsync(db, entityType, ct);
            if (definition == null)
            {
                return Results.NotFound();
            }

            await EnsureCompiledAsync(dynamicEntityService, definition, ct);

            var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(payload.code))
            {
                data["Code"] = payload.code!;
            }
            if (!string.IsNullOrWhiteSpace(payload.name))
            {
                data["Name"] = payload.name!;
            }

            if (payload.fields != null)
            {
                foreach (var field in payload.fields)
                {
                    if (string.IsNullOrWhiteSpace(field.key))
                    {
                        continue;
                    }
                    data[field.key] = field.value ?? DBNull.Value;
                }
            }

            var updated = await persistenceService.UpdateAsync(definition.FullTypeName, id, data);
            if (updated == null)
            {
                return Results.NotFound();
            }

            var dict = ToDictionary(updated);
            return Results.Ok(ToPageLoaderResponse(dict));
        })
        .WithName("UpdateDynamicEntityByShortRoute")
        .WithSummary("按短路由更新动态实体")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        return app;
    }

    private static string NormalizeEntityRoute(string entityPlural)
    {
        var normalized = (entityPlural ?? string.Empty).Trim();
        if (normalized.EndsWith('s') && normalized.Length > 1)
        {
            normalized = normalized[..^1];
        }
        return normalized.ToLowerInvariant();
    }

    private static async Task<EntityDefinition?> ResolveEntityDefinitionAsync(AppDbContext db, string entityRoute, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entityRoute))
        {
            return null;
        }

        var lower = entityRoute.ToLowerInvariant();
        return await db.EntityDefinitions
            .Include(e => e.Fields)
            .Include(e => e.Interfaces)
            .FirstOrDefaultAsync(e => (e.EntityRoute ?? string.Empty).ToLower() == lower, ct);
    }

    private static async Task EnsureCompiledAsync(DynamicEntityService dynamicEntityService, EntityDefinition definition, CancellationToken ct)
    {
        if (dynamicEntityService.GetEntityType(definition.FullTypeName) != null)
        {
            return;
        }

        // 仅在需要时编译；CompileEntityAsync 内部会检查状态与依赖。
        await dynamicEntityService.CompileEntityAsync(definition.Id);
    }

    private static Dictionary<string, object?> ToDictionary(object entity)
    {
        if (entity is Dictionary<string, object?> dict)
        {
            return new Dictionary<string, object?>(dict, StringComparer.OrdinalIgnoreCase);
        }

        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var props = entity.GetType().GetProperties();
        foreach (var p in props)
        {
            result[p.Name] = p.GetValue(entity);
        }
        return result;
    }

    private static object ToPageLoaderResponse(Dictionary<string, object?> dict)
    {
        dict.TryGetValue("Code", out var code);
        dict.TryGetValue("Name", out var name);

        var fields = dict
            .Where(kvp =>
                !string.Equals(kvp.Key, "Code", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(kvp.Key, "Name", StringComparison.OrdinalIgnoreCase))
            .Select(kvp => new { key = kvp.Key, value = kvp.Value })
            .ToList();

        return new
        {
            code = code?.ToString() ?? string.Empty,
            name = name?.ToString() ?? string.Empty,
            fields
        };
    }

    private sealed record DynamicEntityUpdateRequest(string? code, string? name, List<FieldPayload>? fields);
    private sealed record FieldPayload(string key, object? value);
}
