using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Services;
using BobCrm.Api.Base;
using BobCrm.Api.Contracts;
using BobCrm.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace BobCrm.Api.Endpoints;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations").RequireAuthorization();

        group.MapGet("/tree", async (
            [FromServices] OrganizationService service,
            CancellationToken ct) =>
        {
            var data = await service.GetTreeAsync(ct);
            return Results.Ok(data);
        });

        group.MapPost("/", async (
            [FromBody] CreateOrganizationRequest request,
            [FromServices] OrganizationService service,
            ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            return await ExecuteAsync(loc, http, () => service.CreateAsync(request, ct));
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateOrganizationRequest request,
            [FromServices] OrganizationService service,
            ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            return await ExecuteAsync(loc, http, () => service.UpdateAsync(id, request, ct));
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] OrganizationService service,
            ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            return await ExecuteAsync(loc, http, async () =>
            {
                await service.DeleteAsync(id, ct);
                return Results.Ok();
            });
        });

        return app;
    }

    private static async Task<IResult> ExecuteAsync<T>(
        ILocalization loc,
        HttpContext http,
        Func<Task<T>> action)
    {
        var lang = LangHelper.GetLang(http);
        try
        {
            var result = await action();
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new ErrorResponse(string.Format(loc.T("ERR_ORG_OPERATION_FAILED", lang), ex.Message), "ORG_OPERATION_FAILED"));
        }
    }

    private static async Task<IResult> ExecuteAsync(
        ILocalization loc,
        HttpContext http,
        Func<Task<IResult>> action)
    {
        var lang = LangHelper.GetLang(http);
        try
        {
            return await action();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new ErrorResponse(string.Format(loc.T("ERR_ORG_OPERATION_FAILED", lang), ex.Message), "ORG_OPERATION_FAILED"));
        }
    }
}
