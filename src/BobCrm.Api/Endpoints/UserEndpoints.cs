using System.Security.Claims;
using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.DTOs.User;
using BobCrm.Api.Contracts.Requests.User;
using BobCrm.Api.Contracts.Responses.User;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Services;
using BobCrm.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BobCrm.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .RequireAuthorization()
            .WithTags("用户档案")
            .WithOpenApi();
        group.RequireFunction("BAS.AUTH.USERS");

        group.MapGet("", async (
            IUserAppService appService,
            CancellationToken ct) =>
        {
            var list = await appService.GetUsersAsync(ct);
            return Results.Ok(new SuccessResponse<List<UserSummaryDto>>(list));
        })
        .Produces<SuccessResponse<List<UserSummaryDto>>>(StatusCodes.Status200OK);

        group.MapGet("/{id}", async (
            string id,
            IUserAppService appService,
            HttpContext http,
            CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
            var detail = await appService.GetUserAsync(id, lang, ct);
            return Results.Ok(new SuccessResponse<UserDetailDto>(detail));
        })
        .Produces<SuccessResponse<UserDetailDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPost("", async (
            CreateUserRequest request,
            IUserAppService appService,
            HttpContext http,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
            var detail = await appService.CreateUserAsync(request, lang, ct);
            return Results.Ok(new SuccessResponse<UserDetailDto>(detail));
        })
        .Produces<SuccessResponse<UserDetailDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id}", async (
            string id,
            UpdateUserRequest request,
            IUserAppService appService,
            HttpContext http,
            CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
            var detail = await appService.UpdateUserAsync(id, request, lang, ct);
            return Results.Ok(new SuccessResponse<UserDetailDto>(detail));
        })
        .Produces<SuccessResponse<UserDetailDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapPut("/{id}/roles", async (
            string id,
            UpdateUserRolesRequest request,
            IUserAppService appService,
            HttpContext http,
            CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
            var response = await appService.UpdateUserRolesAsync(id, request, lang, ct);
            return Results.Ok(new SuccessResponse<UserRolesUpdateResponse>(response));
        })
        .Produces<SuccessResponse<UserRolesUpdateResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .RequireFunction("BAS.AUTH.USER.ROLE");

        return app;
    }
}
