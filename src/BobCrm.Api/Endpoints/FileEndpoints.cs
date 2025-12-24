using BobCrm.Api.Services.Storage;
using BobCrm.Api.Base;
using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.Responses.File;
using BobCrm.Api.Infrastructure;
using Microsoft.AspNetCore.Authorization;

namespace BobCrm.Api.Endpoints;

public static class FileEndpoints
{
    public static void MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/files").WithTags("File Storage");

        group.MapPost("/upload", [Authorize] async (HttpRequest req, IFileStorageService storage, HttpContext ctx, ILocalization loc) =>
        {
            var lang = LangHelper.GetLang(ctx);
            if (!req.HasFormContentType)
            {
                return Results.BadRequest(new ErrorResponse(loc.T("ERR_FILE_FORM_REQUIRED", lang), "ERR_FILE_FORM_REQUIRED"));
            }
            var form = await req.ReadFormAsync();
            var file = form.Files.GetFile("file");
            if (file == null)
            {
                return Results.BadRequest(new ErrorResponse(loc.T("ERR_FILE_MISSING", lang), "ERR_FILE_MISSING"));
            }
            var prefix = form["prefix"].FirstOrDefault();
            var key = await storage.UploadAsync(file, prefix, ctx.RequestAborted);
            var payload = new FileUploadDto { Key = key, Url = $"/api/files/{Uri.EscapeDataString(key)}" };
            return Results.Ok(new SuccessResponse<FileUploadDto>(payload));
        })
        .DisableAntiforgery()
        .Produces<SuccessResponse<FileUploadDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapGet("/{*key}", async (string key, IFileStorageService storage) =>
        {
            var (stream, contentType) = await storage.GetAsync(key);
            return Results.Stream(stream, contentType);
        });

        group.MapDelete("/{*key}", [Authorize] async (string key, IFileStorageService storage) =>
        {
            await storage.DeleteAsync(key);
            return Results.Ok(ApiResponseExtensions.SuccessResponse());
        });
    }
}

