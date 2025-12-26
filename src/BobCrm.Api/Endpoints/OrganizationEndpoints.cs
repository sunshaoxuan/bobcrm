using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.Requests.Organization;
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
            return Results.Ok(new SuccessResponse<List<OrganizationNodeDto>>(data));
        })
        .Produces<SuccessResponse<List<OrganizationNodeDto>>>(StatusCodes.Status200OK);

        group.MapPost("/", async (
            [FromBody] CreateOrganizationRequest request,
            [FromServices] OrganizationService service,
            ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            var result = await service.CreateAsync(request, ct);
            return Results.Ok(new SuccessResponse<OrganizationNodeDto>(result));
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateOrganizationRequest request,
            [FromServices] OrganizationService service,
            ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            var result = await service.UpdateAsync(id, request, ct);
            return Results.Ok(new SuccessResponse<OrganizationNodeDto>(result));
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] OrganizationService service,
            ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            await service.DeleteAsync(id, ct);
            return Results.Ok(ApiResponseExtensions.SuccessResponse());
        });

        return app;
    }
}
