using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.Requests.Lookups;
using BobCrm.Api.Services;

namespace BobCrm.Api.Endpoints;

public static class LookupEndpoints
{
    public static IEndpointRouteBuilder MapLookupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/lookups")
            .WithTags("Lookups")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapPost("/resolve", async (
            ResolveLookupRequest request,
            LookupResolveService svc,
            CancellationToken ct) =>
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Target) ||
                request.Ids == null ||
                request.Ids.Count == 0)
            {
                return Results.BadRequest(new ErrorResponse("Invalid request", "INVALID_ARGUMENT"));
            }

            var distinct = request.Ids
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(2000)
                .ToList();

            var map = await svc.ResolveAsync(request.Target, distinct, request.DisplayField, ct);
            return Results.Ok(new SuccessResponse<Dictionary<string, string>>(map));
        })
        .WithName("ResolveLookups")
        .WithSummary("Resolve foreign keys to friendly names")
        .Produces<SuccessResponse<Dictionary<string, string>>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        return app;
    }
}

