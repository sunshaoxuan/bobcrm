using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BobCrm.Api.Endpoints;

public static class OrganizationEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organizations").RequireAuthorization();

        group.MapGet("/tree", async ([FromServices] OrganizationService service, CancellationToken ct) =>
        {
            var data = await service.GetTreeAsync(ct);
            return Results.Ok(data);
        });

        group.MapPost("/", async ([FromBody] CreateOrganizationRequest request, [FromServices] OrganizationService service, CancellationToken ct) =>
        {
            return await ExecuteAsync(() => service.CreateAsync(request, ct));
        });

        group.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateOrganizationRequest request, [FromServices] OrganizationService service, CancellationToken ct) =>
        {
            return await ExecuteAsync(() => service.UpdateAsync(id, request, ct));
        });

        group.MapDelete("/{id:guid}", async (Guid id, [FromServices] OrganizationService service, CancellationToken ct) =>
        {
            return await ExecuteAsync(async () =>
            {
                await service.DeleteAsync(id, ct);
                return Results.Ok();
            });
        });

        return app;
    }

    private static async Task<IResult> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            var result = await action();
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> ExecuteAsync(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
