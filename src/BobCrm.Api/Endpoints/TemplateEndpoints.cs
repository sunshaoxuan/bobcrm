using BobCrm.Api.Abstractions;
using System.Linq;
using System.Security.Claims;
using BobCrm.Api.Core.Persistence;
using BobCrm.Api.Core.DomainCommon;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Extensions;
using BobCrm.Api.Services;
using BobCrm.Api.Utils;
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
            ITemplateService templateService,
            string? entityType,
            string? usageType,
            string? templateType,
            string? groupBy) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var result = await templateService.GetTemplatesAsync(uid, entityType, usageType, templateType, groupBy);
            return Results.Json(result);
        })
        .WithName("GetTemplates")
        .WithSummary("获取用户的表单模板列表")
        .WithDescription("支持按实体类型过滤，支持按实体或用户分组");

        // 获取单个模板详情
        group.MapGet("/{id:int}", async (
            int id,
            ClaimsPrincipal user,
            ITemplateService templateService,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var template = await templateService.GetTemplateByIdAsync(id, uid);

            if (template == null)
            {
                return Results.NotFound(new ErrorResponse(loc.T("MSG_TEMPLATE_NOT_FOUND", lang), "TEMPLATE_NOT_FOUND"));
            }

            return Results.Json(template);
        })
        .WithName("GetTemplate")
        .WithSummary("获取模板详情")
        .WithDescription("根据模板ID获取完整的模板信息");

        // 创建新模板
        group.MapPost("", async (
            ClaimsPrincipal user,
            ITemplateService templateService,
            CreateTemplateRequest req) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var template = await templateService.CreateTemplateAsync(uid, req);
            return Results.Created($"/api/templates/{template.Id}", template);
        })
        .WithName("CreateTemplate")
        .WithSummary("创建新模板")
        .WithDescription("创建一个新的表单模板");

        // 更新模板
        group.MapPut("/{id:int}", async (
            int id,
            ClaimsPrincipal user,
            ITemplateService templateService,
            UpdateTemplateRequest req,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            try
            {
                var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
                var template = await templateService.UpdateTemplateAsync(id, uid, req);
                return Results.Ok(template);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new ErrorResponse(loc.T("MSG_TEMPLATE_NOT_FOUND", lang), "TEMPLATE_NOT_FOUND"));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse(
                    string.Format(loc.T("ERR_TEMPLATE_OPERATION_FAILED", lang), ex.Message),
                    "TEMPLATE_UPDATE_FAILED"));
            }
        })
        .WithName("UpdateTemplate")
        .WithSummary("更新模板")
        .WithDescription("更新模板信息（EntityType一旦设置后不允许修改）");

        // 删除模板
        group.MapDelete("/{id:int}", async (
            int id,
            ClaimsPrincipal user,
            ITemplateService templateService,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            try
            {
                var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
                await templateService.DeleteTemplateAsync(id, uid);
                return Results.Ok(ApiResponseExtensions.SuccessResponse(loc.T("MSG_TEMPLATE_DELETED", lang)));
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new ErrorResponse(loc.T("MSG_TEMPLATE_NOT_FOUND", lang), "TEMPLATE_NOT_FOUND"));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse(
                    string.Format(loc.T("ERR_TEMPLATE_OPERATION_FAILED", lang), ex.Message),
                    "TEMPLATE_DELETE_FAILED"));
            }
        })
        .WithName("DeleteTemplate")
        .WithSummary("删除模板")
        .WithDescription("删除模板（系统默认、用户默认和正在使用的模板不允许删除）");

        // 复制模板
        group.MapPost("/{id:int}/copy", async (
            int id,
            ClaimsPrincipal user,
            ITemplateService templateService,
            CopyTemplateRequest req,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            try
            {
                var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
                var newTemplate = await templateService.CopyTemplateAsync(id, uid, req);
                return Results.Created($"/api/templates/{newTemplate.Id}", newTemplate);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new ErrorResponse(loc.T("MSG_TEMPLATE_NOT_FOUND", lang), "TEMPLATE_NOT_FOUND"));
            }
        })
        .WithName("CopyTemplate")
        .WithSummary("复制模板")
        .WithDescription("从现有模板创建副本（可以从系统模板或用户模板复制）");

        // 应用模板（设置为用户默认模板）
        group.MapPut("/{id:int}/apply", async (
            int id,
            ClaimsPrincipal user,
            ITemplateService templateService,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            try
            {
                var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
                var template = await templateService.ApplyTemplateAsync(id, uid);

                return Results.Ok(new
                {
                    message = loc.T("MSG_TEMPLATE_APPLIED_DEFAULT", lang),
                    template = new
                    {
                        template.Id,
                        template.Name,
                        template.EntityType,
                        usageType = "Unknown", // FormTemplate 不再包含 UsageType，需通过 TemplateStateBinding 查询
                        template.IsUserDefault,
                        template.IsSystemDefault
                    }
                });
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new ErrorResponse(loc.T("MSG_TEMPLATE_NOT_FOUND", lang), "TEMPLATE_NOT_FOUND"));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse(
                    string.Format(loc.T("ERR_TEMPLATE_OPERATION_FAILED", lang), ex.Message),
                    "TEMPLATE_APPLY_FAILED"));
            }
        })
        .WithName("ApplyTemplate")
        .WithSummary("应用模板")
        .WithDescription("将模板设置为用户默认模板（如果是系统模板，会先创建副本）");

        // 获取有效模板（用于PageLoader）
        group.MapGet("/effective/{entityType}", async (
            string entityType,
            ClaimsPrincipal user,
            ITemplateService templateService,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var template = await templateService.GetEffectiveTemplateAsync(entityType, uid);

            if (template == null)
            {
                return Results.NotFound(new ErrorResponse(loc.T("MSG_TEMPLATE_NOT_FOUND", lang), "TEMPLATE_NOT_FOUND"));
            }

            return Results.Json(template);
        })
        .WithName("GetEffectiveTemplate")
        .WithSummary("获取有效模板")
        .WithDescription("按优先级获取模板：用户默认 > 系统默认 > 第一个创建的模板");

        group.MapGet("/menu-bindings", async (
            string? lang,
            ClaimsPrincipal user,
            AppDbContext db,
            ILocalization loc,
            HttpContext http,
            ILogger<Program> logger,
            string? viewState,
            CancellationToken ct) =>
        {
            var targetLang = LangHelper.GetLang(http, lang);
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(uid))
            {
                return Results.Unauthorized();
            }

            var resolvedViewState = viewState ?? "DetailView";
            var now = DateTime.UtcNow;

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
                .Include(fn => fn.TemplateStateBinding!)
                .ThenInclude(tsb => tsb.Template)
                .Where(fn => fn.TemplateStateBindingId != null && accessibleFunctionIds.Contains(fn.Id))
                .ToListAsync(ct);

            var filteredNodes = menuNodes
                .Where(fn => fn.TemplateStateBinding != null && fn.TemplateStateBinding.ViewState == resolvedViewState)
                .OrderBy(fn => fn.SortOrder)
                .ToList();

            if (filteredNodes.Count == 0)
            {
                return Results.Ok(Array.Empty<object>());
            }

            var entityTypes = filteredNodes
                .Select(fn => fn.TemplateStateBinding!.EntityType)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var entityTypeSet = new HashSet<string>(entityTypes, StringComparer.OrdinalIgnoreCase);

            var entityMetadata = await db.EntityDefinitions
                .AsNoTracking()
                .Where(ed => ed.EntityRoute != null && entityTypeSet.Contains(ed.EntityRoute))
                .ToDictionaryAsync(
                    ed => ed.EntityRoute!,
                    ed =>
                    {
                        var summary = ed.ToSummaryDto(targetLang);
                        var displayNameSingle = summary.DisplayName
                            ?? summary.DisplayNameTranslations?.Resolve(targetLang ?? string.Empty)
                            ?? ed.EntityName;
                        return new EntityMenuMetadata(
                            displayNameSingle,
                            summary.DisplayNameTranslations,
                            ResolveRoute(ed));
                    },
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
                var binding = node.TemplateStateBinding!;
                var key = binding.EntityType;
                var usageType = binding.ViewState switch
                {
                    "List" => FormTemplateUsageType.List,
                    "DetailEdit" => FormTemplateUsageType.Edit,
                    "Create" => FormTemplateUsageType.Combined,
                    _ => FormTemplateUsageType.Detail
                };
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
                var displayNameTranslations = metadata?.DisplayNameTranslations ??
                    (node.DisplayName == null ? null : new MultilingualText(node.DisplayName));
                var resolvedMenuName = !string.IsNullOrWhiteSpace(targetLang)
                    ? (displayNameTranslations?.Resolve(targetLang) ?? displayName ?? node.Name)
                    : null;

                var menuPayload = new
                {
                    node.Id,
                    Code = NormalizeMenuCode(node.Code),
                    Name = resolvedMenuName ?? displayName,
                    node.DisplayNameKey,
                    DisplayName = string.IsNullOrWhiteSpace(targetLang)
                        ? null
                        : resolvedMenuName ?? displayName,
                    DisplayNameTranslations = string.IsNullOrWhiteSpace(targetLang)
                        ? displayNameTranslations
                        : null,
                    Route = resolvedRoute,
                    node.Icon,
                    node.SortOrder
                };

                var entry = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Menu"] = menuPayload,
                    ["Binding"] = new
                    {
                        binding.Id,
                        binding.EntityType,
                        viewState = binding.ViewState,
                        usageType,
                        binding.TemplateId,
                        isDefault = binding.IsDefault,
                        requiredPermission = binding.RequiredPermission
                    },
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
            ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
            var resolvedUsage = usageType ?? FormTemplateUsageType.Detail;
            var binding = await bindingService.GetBindingAsync(entityType, resolvedUsage, ct);
            return binding is null
                ? Results.NotFound(new ErrorResponse(loc.T("ERR_TEMPLATE_BINDING_NOT_FOUND", lang), "TEMPLATE_BINDING_NOT_FOUND"))
                : Results.Ok(binding.ToDto());
        })
        .WithName("GetTemplateBinding")
        .WithSummary("获取实体模板绑定")
        .WithDescription("按照实体与用途获取模板绑定记录。");

        group.MapPut("/bindings", async (
            UpsertTemplateBindingRequest request,
            ClaimsPrincipal user,
            TemplateBindingService bindingService,
            ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
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
            ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(uid))
            {
                return Results.Unauthorized();
            }

            request ??= new TemplateRuntimeRequest();
            try
            {
                var context = await runtimeService.BuildRuntimeContextAsync(uid, entityType, request, ct);
                return Results.Ok(context);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound(new ErrorResponse(loc.T("MSG_TEMPLATE_NOT_FOUND", lang), "TEMPLATE_NOT_FOUND"));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse(
                    string.Format(loc.T("ERR_TEMPLATE_OPERATION_FAILED", lang), ex.Message),
                    "TEMPLATE_RUNTIME_FAILED"));
            }
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

    private sealed record EntityMenuMetadata(string DisplayName, MultilingualText? DisplayNameTranslations, string? Route);
}
