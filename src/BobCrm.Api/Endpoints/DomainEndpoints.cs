using BobCrm.Api.Base.Models;
using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.Responses.Entity;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace BobCrm.Api.Endpoints;

public static class DomainEndpoints
{
    public static IEndpointRouteBuilder MapDomainEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/entity-domains")
            .RequireAuthorization()
            .WithTags("Entity Domains")
            .WithOpenApi();

        group.MapGet("/", async (
            HttpContext http,
            [FromQuery] string? lang,
            AppDbContext db) =>
        {
            var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);

            var domains = await db.EntityDomains
                .AsNoTracking()
                .Where(d => d.IsEnabled)
                .OrderBy(d => d.SortOrder)
                .ThenBy(d => d.Code)
                .Select(d => new EntityDomainDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    Name = targetLang != null ? d.Name.Resolve(targetLang) : null,
                    NameTranslations = targetLang == null ? new MultilingualText(d.Name) : null,
                    SortOrder = d.SortOrder,
                    IsSystem = d.IsSystem
                })
                .ToListAsync();

            return Results.Ok(new SuccessResponse<List<EntityDomainDto>>(domains));
        })
        .WithName("GetEntityDomains")
        .WithSummary("获取实体领域列表")
        .WithDescription("返回包含多语言名称的可用实体领域。")
        .Produces<SuccessResponse<List<EntityDomainDto>>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", async (
            Guid id,
            HttpContext http,
            [FromQuery] string? lang,
            AppDbContext db,
            ILocalization loc) =>
        {
            var targetLang = string.IsNullOrWhiteSpace(lang) ? null : LangHelper.GetLang(http, lang);
            var uiLang = LangHelper.GetLang(http);

            var domain = await db.EntityDomains
                .AsNoTracking()
                .Where(d => d.IsEnabled)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (domain == null)
            {
                return Results.NotFound(new ErrorResponse(loc.T("ERR_ENTITY_NOT_FOUND", uiLang), "ENTITY_DOMAIN_NOT_FOUND"));
            }

            return Results.Ok(new SuccessResponse<EntityDomainDto>(new EntityDomainDto
            {
                Id = domain.Id,
                Code = domain.Code,
                Name = targetLang != null ? domain.Name.Resolve(targetLang) : null,
                NameTranslations = targetLang == null ? new MultilingualText(domain.Name) : null,
                SortOrder = domain.SortOrder,
                IsSystem = domain.IsSystem
            }));
        })
        .WithName("GetEntityDomainById")
        .WithSummary("获取实体领域详情")
        .Produces<SuccessResponse<EntityDomainDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        return app;
    }
}
