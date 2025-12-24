using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.Responses.System;
using BobCrm.Api.Infrastructure;
using BobCrm.Api.Middleware;
using BobCrm.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BobCrm.Api.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/system")
            .RequireAuthorization()
            .WithTags("系统治理")
            .WithOpenApi();

        group.MapGet("/audit-logs", async (
            int page,
            int pageSize,
            string? module,
            string? operationType,
            string? actorId,
            DateTime? fromUtc,
            DateTime? toUtc,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (page < 1 || pageSize < 1 || pageSize > 200)
            {
                return Results.BadRequest(new ErrorResponse("Invalid pagination parameters", "INVALID_PAGINATION"));
            }

            var query = db.AuditLogs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(module))
            {
                query = query.Where(x => x.Module == module);
            }

            if (!string.IsNullOrWhiteSpace(operationType))
            {
                query = query.Where(x => x.OperationType == operationType);
            }

            if (!string.IsNullOrWhiteSpace(actorId))
            {
                query = query.Where(x => x.ActorId == actorId);
            }

            if (fromUtc.HasValue)
            {
                query = query.Where(x => x.OccurredAt >= fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(x => x.OccurredAt <= toUtc.Value);
            }

            var totalCount = await query.LongCountAsync(ct);
            var items = await query
                .OrderByDescending(x => x.OccurredAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AuditLogDto
                {
                    Id = x.Id,
                    Module = x.Module,
                    OperationType = x.OperationType,
                    ActorId = x.ActorId,
                    ActorName = x.ActorName,
                    IpAddress = x.IpAddress,
                    Target = x.Target,
                    Description = x.Description,
                    BeforeJson = x.BeforeJson,
                    AfterJson = x.AfterJson,
                    ChangesJson = x.ChangesJson,
                    OccurredAt = x.OccurredAt
                })
                .ToListAsync(ct);

            return Results.Ok(new PagedResponse<AuditLogDto>(items, page, pageSize, totalCount));
        })
        .Produces<PagedResponse<AuditLogDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapGet("/info", (
            SystemRuntimeInfo runtimeInfo,
            AppDbContext db) =>
        {
            var provider = db.Database.ProviderName ?? "unknown";
            var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
            var workingSet = Process.GetCurrentProcess().WorkingSet64;

            var dto = new SystemInfoDto
            {
                StartedAtUtc = runtimeInfo.StartedAtUtc,
                WorkingSetBytes = workingSet,
                Version = version,
                DbProvider = provider
            };

            return Results.Ok(new SuccessResponse<SystemInfoDto>(dto));
        })
        .RequireFunction("SYS.ADMIN")
        .Produces<SuccessResponse<SystemInfoDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized)
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden);

        return app;
    }
}
