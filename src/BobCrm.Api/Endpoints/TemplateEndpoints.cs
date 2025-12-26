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
using BobCrm.Api.Contracts.DTOs.Template;
using BobCrm.Api.Contracts.Requests.Template;
using BobCrm.Api.Contracts.Responses.Template;
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
            return Results.Ok(new SuccessResponse<TemplateQueryResponseDto>(result));
        })
        .WithName("GetTemplates")
        .WithSummary("获取用户的表单模板列表")
        .WithDescription("支持按实体类型过滤，支持按实体或用户分组")
        .Produces<SuccessResponse<TemplateQueryResponseDto>>(StatusCodes.Status200OK);

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

            return Results.Ok(new SuccessResponse<FormTemplate>(template));
        })
        .WithName("GetTemplate")
        .WithSummary("获取模板详情")
        .WithDescription("根据模板ID获取完整的模板信息")
        .Produces<SuccessResponse<FormTemplate>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // 创建新模板
        group.MapPost("", async (
            ClaimsPrincipal user,
            ITemplateService templateService,
            CreateTemplateRequest req) =>
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var template = await templateService.CreateTemplateAsync(uid, req);
            return Results.Created($"/api/templates/{template.Id}", new SuccessResponse<FormTemplate>(template));
        })
        .WithName("CreateTemplate")
        .WithSummary("创建新模板")
        .WithDescription("创建一个新的表单模板")
        .Produces<SuccessResponse<FormTemplate>>(StatusCodes.Status201Created);

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
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var template = await templateService.UpdateTemplateAsync(id, uid, req);
            return Results.Ok(new SuccessResponse<FormTemplate>(template));
        })
        .WithName("UpdateTemplate")
        .WithSummary("更新模板")
        .WithDescription("更新模板信息（EntityType一旦设置后不允许修改）")
        .Produces<SuccessResponse<FormTemplate>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // 删除模板
        group.MapDelete("/{id:int}", async (
            int id,
            ClaimsPrincipal user,
            ITemplateService templateService,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            await templateService.DeleteTemplateAsync(id, uid);
            return Results.Ok(ApiResponseExtensions.SuccessResponse(loc.T("MSG_TEMPLATE_DELETED", lang)));
        })
        .WithName("DeleteTemplate")
        .WithSummary("删除模板")
        .WithDescription("删除模板（系统默认、用户默认和正在使用的模板不允许删除）")
        .Produces<SuccessResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

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
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var newTemplate = await templateService.CopyTemplateAsync(id, uid, req);
            return Results.Created($"/api/templates/{newTemplate.Id}", new SuccessResponse<FormTemplate>(newTemplate));
        })
        .WithName("CopyTemplate")
        .WithSummary("复制模板")
        .WithDescription("从现有模板创建副本（可以从系统模板或用户模板复制）")
        .Produces<SuccessResponse<FormTemplate>>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // 应用模板（设置为用户默认模板）
        group.MapPut("/{id:int}/apply", async (
            int id,
            ClaimsPrincipal user,
            ITemplateService templateService,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var template = await templateService.ApplyTemplateAsync(id, uid);

            var payload = new ApplyTemplateResultDto
            {
                Message = loc.T("MSG_TEMPLATE_APPLIED_DEFAULT", lang),
                Template = new AppliedTemplateDto
                {
                    Id = template.Id,
                    Name = template.Name,
                    EntityType = template.EntityType,
                    UsageType = "Unknown",
                    IsUserDefault = template.IsUserDefault,
                    IsSystemDefault = template.IsSystemDefault
                }
            };

            return Results.Ok(new SuccessResponse<ApplyTemplateResultDto>(payload));
        })
        .WithName("ApplyTemplate")
        .WithSummary("应用模板")
        .WithDescription("将模板设置为用户默认模板（如果是系统模板，会先创建副本）")
        .Produces<SuccessResponse<ApplyTemplateResultDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

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

            return Results.Ok(new SuccessResponse<FormTemplate>(template));
        })
        .WithName("GetEffectiveTemplate")
        .WithSummary("获取有效模板")
        .WithDescription("按优先级获取模板：用户默认 > 系统默认 > 第一个创建的模板")
        .Produces<SuccessResponse<FormTemplate>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/menu-bindings", async (
            string? lang,
            ClaimsPrincipal user,
            ITemplateBindingAppService bindingAppService,
            HttpContext http,
            FormTemplateUsageType? usageType,
            string? viewState,
            CancellationToken ct) =>
        {
            var targetLang = LangHelper.GetLang(http, lang);
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(uid))
            {
                return Results.Unauthorized();
            }

            var resolvedViewState = !string.IsNullOrWhiteSpace(viewState)
                ? viewState!
                : usageType switch
                {
                    FormTemplateUsageType.List => "List",
                    FormTemplateUsageType.Edit => "DetailEdit",
                    FormTemplateUsageType.Combined => "Create",
                    _ => "DetailView"
                };

            var result = await bindingAppService.GetMenuTemplateIntersectionsAsync(uid, targetLang, resolvedViewState, ct);
            return Results.Ok(new SuccessResponse<List<MenuTemplateIntersectionDto>>(result));
        })
        .WithName("GetMenuTemplateIntersections")
        .WithSummary("获取菜单与模板交集")
        .WithDescription("返回当前用户可访问的菜单节点及其模板绑定和可选模板列表。")
        .Produces<SuccessResponse<List<MenuTemplateIntersectionDto>>>(StatusCodes.Status200OK);

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
#pragma warning disable CS0618
                : Results.Ok(new SuccessResponse<TemplateBindingDto>(binding.ToDto()));
#pragma warning restore CS0618
        })
        .WithName("GetTemplateBinding")
        .WithSummary("获取实体模板绑定")
        .WithDescription("按照实体与用途获取模板绑定记录。")
        .Produces<SuccessResponse<TemplateBindingDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

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

#pragma warning disable CS0618
            return Results.Ok(new SuccessResponse<TemplateBindingDto>(binding.ToDto()));
#pragma warning restore CS0618
        })
        .WithName("UpsertTemplateBinding")
        .WithSummary("更新模板绑定")
        .WithDescription("创建或更新实体与模板之间的绑定关系。")
        .Produces<SuccessResponse<TemplateBindingDto>>(StatusCodes.Status200OK);

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
            var context = await runtimeService.BuildRuntimeContextAsync(uid, entityType, request, ct);
            return Results.Ok(new SuccessResponse<TemplateRuntimeResponse>(context));
        })
        .WithName("BuildTemplateRuntime")
        .WithSummary("获取模板运行时上下文")
        .WithDescription("结合权限与数据范围返回模板所需的运行时信息。")
        .Produces<SuccessResponse<TemplateRuntimeResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        return app;
    }




}
