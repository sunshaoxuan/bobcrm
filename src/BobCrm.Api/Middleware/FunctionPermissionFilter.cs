using System.Security.Claims;
using BobCrm.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace BobCrm.Api.Middleware;

public sealed class FunctionPermissionFilter : IEndpointFilter
{
    private readonly string _functionCode;

    public FunctionPermissionFilter(string functionCode)
    {
        _functionCode = functionCode;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (string.IsNullOrWhiteSpace(_functionCode))
        {
            return await next(context);
        }

        var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Unauthorized();
        }

        var accessService = context.HttpContext.RequestServices.GetRequiredService<AccessService>();
        var allowed = await accessService.HasFunctionAccessAsync(userId, _functionCode, context.HttpContext.RequestAborted);
        if (!allowed)
        {
            return Results.Forbid();
        }

        return await next(context);
    }
}

public static class EndpointFunctionExtensions
{
    public static RouteHandlerBuilder RequireFunction(this RouteHandlerBuilder builder, string functionCode)
    {
        return builder.AddEndpointFilter(new FunctionPermissionFilter(functionCode));
    }

    public static RouteGroupBuilder RequireFunction(this RouteGroupBuilder builder, string functionCode)
    {
        builder.AddEndpointFilter(new FunctionPermissionFilter(functionCode));
        return builder;
    }
}
