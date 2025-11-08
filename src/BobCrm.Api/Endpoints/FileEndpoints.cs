using BobCrm.Api.Services.Storage;
using Microsoft.AspNetCore.Authorization;

namespace BobCrm.Api.Endpoints;

public static class FileEndpoints
{
    public static void MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/files").WithTags("文件存储");

        group.MapPost("/upload", [Authorize] async (HttpRequest req, IFileStorageService storage, HttpContext ctx) =>
        {
            if (!req.HasFormContentType) return Results.BadRequest(new { message = "multipart/form-data required" });
            var form = await req.ReadFormAsync();
            var file = form.Files.GetFile("file");
            if (file == null) return Results.BadRequest(new { message = "missing file" });
            var prefix = form["prefix"].FirstOrDefault();
            var key = await storage.UploadAsync(file, prefix, ctx.RequestAborted);
            return Results.Ok(new { key, url = $"/api/files/{Uri.EscapeDataString(key)}" });
        })
        .DisableAntiforgery();

        group.MapGet("/{*key}", async (string key, IFileStorageService storage) =>
        {
            var (stream, contentType) = await storage.GetAsync(key);
            return Results.Stream(stream, contentType);
        });

        group.MapDelete("/{*key}", [Authorize] async (string key, IFileStorageService storage) =>
        {
            await storage.DeleteAsync(key);
            return Results.NoContent();
        });
    }
}

