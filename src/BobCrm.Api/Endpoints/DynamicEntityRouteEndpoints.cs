using System.Security.Claims;
using System.Text.Json;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.Requests.Template;
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
            int? tid,
            Guid? mid,
            string? vs,
            ClaimsPrincipal user,
            AppDbContext db,
            DynamicEntityService dynamicEntityService,
            IReflectionPersistenceService persistenceService,
            TemplateRuntimeService templateRuntimeService,
            AccessService accessService,
            ILocalization loc,
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

            // FIX-10 (SEC-05): 强制字段剪裁 - 任何情况下都不可被 querystring 绕过。
            // 规则：
            // - 如果给定 mid/tid：按指定上下文解析模板并剪裁（并执行权限校验）
            // - 否则：根据“用户可访问的菜单节点”聚合允许字段；若无法解析则降级为最小返回（Code/Name/Id）
            var lang = LangHelper.GetLang(http);
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid))
            {
                return Results.Unauthorized();
            }

            JsonElement? entityData = null;
            try
            {
                entityData = JsonSerializer.SerializeToElement(dict, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            }
            catch
            {
                entityData = null;
            }

            HashSet<string> allowedFields = new(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (tid.HasValue || mid.HasValue)
                {
                    var ctx = await templateRuntimeService.BuildRuntimeContextAsync(
                        uid,
                        entityType,
                        new TemplateRuntimeRequest(
                            UsageType: FormTemplateUsageType.Detail,
                            TemplateId: tid,
                            ViewState: null,
                            MenuNodeId: mid,
                            FunctionCodeOverride: null,
                            EntityId: id,
                            EntityData: entityData),
                        ct);
                    allowedFields = ExtractTemplateFields(ctx.Template.LayoutJson);
                }
                else
                {
                    // No explicit context: infer allowed fields from all accessible menu nodes for this entity/view state.
                    var viewState = "DetailView";
                    var candidates = await (
                        from n in db.FunctionNodes.AsNoTracking()
                        join b in db.TemplateStateBindings.AsNoTracking()
                            on n.TemplateStateBindingId equals b.Id
                        where n.TemplateStateBindingId.HasValue
                              && (b.EntityType == entityType || b.EntityType == entityType + "s")
                              && b.ViewState == viewState
                        select new { n.Code, b.TemplateId }
                    ).ToListAsync(ct);

                    var templateIds = candidates.Select(c => c.TemplateId).Distinct().ToList();
                    var templates = await db.FormTemplates
                        .AsNoTracking()
                        .Where(t => templateIds.Contains(t.Id))
                        .Select(t => new { t.Id, t.LayoutJson })
                        .ToListAsync(ct);
                    var templateLayoutMap = templates.ToDictionary(t => t.Id, t => t.LayoutJson);

                    foreach (var c in candidates)
                    {
                        if (!await accessService.HasFunctionAccessAsync(uid, c.Code, ct))
                        {
                            continue;
                        }

                        templateLayoutMap.TryGetValue(c.TemplateId, out var layoutJson);
                        var fields = ExtractTemplateFields(layoutJson);
                        foreach (var f in fields)
                        {
                            allowedFields.Add(f);
                        }
                    }

                    // Fallback: if we still don't have any allowed fields, use effective runtime template (if available)
                    if (allowedFields.Count == 0)
                    {
                        try
                        {
                            var ctx = await templateRuntimeService.BuildRuntimeContextAsync(
                                uid,
                                entityType,
                                new TemplateRuntimeRequest(
                                    UsageType: FormTemplateUsageType.Detail,
                                    TemplateId: null,
                                    ViewState: null,
                                    MenuNodeId: null,
                                    FunctionCodeOverride: null,
                                    EntityId: id,
                                    EntityData: entityData),
                                ct);
                            allowedFields = ExtractTemplateFields(ctx.Template.LayoutJson);
                        }
                        catch
                        {
                            allowedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Json(
                    new ErrorResponse(loc.T("ERR_FORBIDDEN", lang), "FORBIDDEN"),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            // Always filter: minimum fields are safe and required by runtime.
            dict = dict
                .Where(kvp =>
                    string.Equals(kvp.Key, "Code", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kvp.Key, "Name", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kvp.Key, "Id", StringComparison.OrdinalIgnoreCase) ||
                    allowedFields.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

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

    private static HashSet<string> ExtractTemplateFields(string? layoutJson)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(layoutJson))
        {
            return set;
        }

        try
        {
            using var doc = JsonDocument.Parse(layoutJson);
            Walk(doc.RootElement, set);
        }
        catch
        {
            // ignore parse errors: safest fallback is "no filter" handled by caller (empty set)
        }

        return set;

        static void Walk(JsonElement el, HashSet<string> acc)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in el.EnumerateObject())
                    {
                        if (string.Equals(prop.Name, "dataField", StringComparison.OrdinalIgnoreCase) &&
                            prop.Value.ValueKind == JsonValueKind.String)
                        {
                            var v = prop.Value.GetString();
                            if (!string.IsNullOrWhiteSpace(v))
                            {
                                acc.Add(v!);
                            }
                        }
                        Walk(prop.Value, acc);
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (var item in el.EnumerateArray())
                    {
                        Walk(item, acc);
                    }
                    break;
            }
        }
    }
}
