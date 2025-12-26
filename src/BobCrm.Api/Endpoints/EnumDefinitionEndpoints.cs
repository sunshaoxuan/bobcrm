using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.DTOs.Enum;
using BobCrm.Api.Contracts.Requests.Enum;
using BobCrm.Api.Services;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace BobCrm.Api.Endpoints;

/// <summary>
/// 枚举定义管理 API 端点
/// </summary>
public static class EnumDefinitionEndpoints
{
    public static void MapEnumDefinitionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/enums")
            .WithTags("Enums")
            .RequireAuthorization();

        // GET /api/enums - 获取所有枚举定义
        group.MapGet("/", async (
            HttpContext http,
            [FromQuery] string? lang,
            [FromServices] EnumDefinitionService service,
            [FromQuery] bool includeDisabled = false) =>
        {
            var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
            var enums = await service.GetAllAsync(includeDisabled, targetLang);
            return Results.Ok(new SuccessResponse<List<EnumDefinitionDto>>(enums));
        })
        .WithName("GetAllEnums")
        .WithSummary("获取所有枚举定义")
        .Produces<SuccessResponse<List<EnumDefinitionDto>>>(StatusCodes.Status200OK);

        // GET /api/enums/{id} - 根据ID获取枚举定义
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromQuery] string? lang,
            [FromServices] EnumDefinitionService service,
            ILocalization loc,
            HttpContext http) =>
        {
            var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
            var uiLang = LangHelper.GetLang(http);
            var enumDef = await service.GetByIdAsync(id, targetLang);
            return enumDef == null
                ? Results.NotFound(new ErrorResponse(loc.T("ERR_ENUM_NOT_FOUND", uiLang), "ENUM_NOT_FOUND"))
                : Results.Ok(new SuccessResponse<EnumDefinitionDto>(enumDef));
        })
        .WithName("GetEnumById")
        .WithSummary("根据ID获取枚举定义")
        .Produces<SuccessResponse<EnumDefinitionDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // GET /api/enums/by-code/{code} - 根据Code获取枚举定义
        group.MapGet("/by-code/{code}", async (
            string code,
            [FromQuery] string? lang,
            [FromServices] EnumDefinitionService service,
            ILocalization loc,
            HttpContext http) =>
        {
            var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
            var uiLang = LangHelper.GetLang(http);
            var enumDef = await service.GetByCodeAsync(code, targetLang);
            return enumDef == null
                ? Results.NotFound(new ErrorResponse(loc.T("ERR_ENUM_NOT_FOUND", uiLang), "ENUM_NOT_FOUND"))
                : Results.Ok(new SuccessResponse<EnumDefinitionDto>(enumDef));
        })
        .WithName("GetEnumByCode")
        .WithSummary("根据Code获取枚举定义")
        .Produces<SuccessResponse<EnumDefinitionDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // GET /api/enums/{id}/options - 获取枚举的所有选项
        group.MapGet("/{id:guid}/options", async (
            Guid id,
            [FromQuery] string? lang,
            [FromServices] EnumDefinitionService service,
            ILocalization loc,
            HttpContext http,
            [FromQuery] bool includeDisabled = false) =>
        {
            var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
            var options = await service.GetOptionsAsync(id, includeDisabled, targetLang);
            return Results.Ok(new SuccessResponse<List<EnumOptionDto>>(options));
        })
        .WithName("GetEnumOptions")
        .WithSummary("获取枚举的所有选项")
        .Produces<SuccessResponse<List<EnumOptionDto>>>(StatusCodes.Status200OK);

        // POST /api/enums - 创建枚举定义
        group.MapPost("/", async (
            [FromBody] CreateEnumDefinitionRequest request,
            [FromServices] EnumDefinitionService service,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
                var created = await service.CreateAsync(request);
                return Results.Created($"/api/enums/{created.Id}", new SuccessResponse<EnumDefinitionDto>(created));
        })
        .WithName("CreateEnum")
        .WithSummary("创建枚举定义")
        .Produces<SuccessResponse<EnumDefinitionDto>>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        // PUT /api/enums/{id} - 更新枚举定义
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateEnumDefinitionRequest request,
            [FromServices] EnumDefinitionService service,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
                var updated = await service.UpdateAsync(id, request);
                return updated == null
                    ? Results.NotFound(new ErrorResponse(loc.T("ERR_ENUM_NOT_FOUND", lang), "ENUM_NOT_FOUND"))
                    : Results.Ok(new SuccessResponse<EnumDefinitionDto>(updated));
        })
        .WithName("UpdateEnum")
        .WithSummary("更新枚举定义")
        .Produces<SuccessResponse<EnumDefinitionDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        // DELETE /api/enums/{id} - 删除枚举定义
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] EnumDefinitionService service,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
                var deleted = await service.DeleteAsync(id);
                return deleted
                    ? Results.Ok(ApiResponseExtensions.SuccessResponse())
                    : Results.NotFound(new ErrorResponse(loc.T("ERR_ENUM_NOT_FOUND", lang), "ENUM_NOT_FOUND"));
        })
        .WithName("DeleteEnum")
        .WithSummary("删除枚举定义")
        .Produces<SuccessResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        // PUT /api/enums/{id}/options - 批量更新枚举选项
        group.MapPut("/{id:guid}/options", async (
            Guid id,
            [FromBody] UpdateEnumOptionsRequest request,
            [FromServices] EnumDefinitionService service,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
                var options = await service.UpdateOptionsAsync(id, request);
                return Results.Ok(new SuccessResponse<List<EnumOptionDto>>(options));
        })
        .WithName("UpdateEnumOptions")
        .WithSummary("批量更新枚举选项")
        .Produces<SuccessResponse<List<EnumOptionDto>>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);
    }
}
