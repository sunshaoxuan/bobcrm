using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.Responses.System;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Contracts.Requests.I18n;
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
            string? actor,
            string? actorId,
            DateTime? fromUtc,
            DateTime? toUtc,
            AuditLogService auditLogs,
            ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            if (page < 1 || pageSize < 1 || pageSize > 200)
            {
                var lang = LangHelper.GetLang(http);
                return Results.BadRequest(new ErrorResponse(loc.T("ERR_INVALID_PAGINATION", lang), "INVALID_PAGINATION"));
            }

            var actorQuery = !string.IsNullOrWhiteSpace(actor) ? actor : actorId;
            var result = await auditLogs.SearchAsync(page, pageSize, module, operationType, actorQuery, fromUtc, toUtc, ct);
            return Results.Ok(result);
        })
        .RequireFunction("SYS.AUDIT")
        .Produces<PagedResponse<AuditLogDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapGet("/audit-logs/modules", async (
            int? limit,
            AuditLogService auditLogs,
            CancellationToken ct) =>
        {
            var result = await auditLogs.GetModulesAsync(limit ?? 200, ct);
            return Results.Ok(result);
        })
        .RequireFunction("SYS.AUDIT")
        .Produces<List<string>>(StatusCodes.Status200OK);

        group.MapGet("/jobs", async (
            int page,
            int pageSize,
            IBackgroundJobClient jobs,
            ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            if (page < 1 || pageSize < 1 || pageSize > 200)
            {
                var lang = LangHelper.GetLang(http);
                return Results.BadRequest(new ErrorResponse(loc.T("ERR_INVALID_PAGINATION", lang), "INVALID_PAGINATION"));
            }

            var result = await jobs.GetRecentJobsAsync(page, pageSize, ct);
            return Results.Ok(result);
        })
        .RequireFunction("SYS.JOBS")
        .Produces<PagedResponse<BackgroundJobDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapGet("/jobs/{id:guid}", async (
            Guid id,
            IBackgroundJobClient jobs,
            CancellationToken ct) =>
        {
            var job = await jobs.GetJobAsync(id, ct);
            return job == null ? Results.NotFound() : Results.Ok(new SuccessResponse<BackgroundJobDto>(job));
        })
        .RequireFunction("SYS.JOBS")
        .Produces<SuccessResponse<BackgroundJobDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/jobs/{id:guid}/logs", async (
            Guid id,
            int? limit,
            IBackgroundJobClient jobs,
            CancellationToken ct) =>
        {
            var logs = await jobs.GetJobLogsAsync(id, limit ?? 500, ct);
            return Results.Ok(new SuccessResponse<IReadOnlyList<BackgroundJobLogDto>>(logs));
        })
        .RequireFunction("SYS.JOBS")
        .Produces<SuccessResponse<IReadOnlyList<BackgroundJobLogDto>>>(StatusCodes.Status200OK);

        group.MapPost("/jobs/{id:guid}/cancel", async (
            Guid id,
            IBackgroundJobClient jobs,
            ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
            var ok = await jobs.RequestCancelAsync(id, ct);
            return ok ? Results.Ok(new SuccessResponse(loc.T("MSG_JOB_CANCEL_REQUESTED", lang))) : Results.BadRequest(new ErrorResponse(loc.T("ERR_JOB_NOT_CANCELLABLE", lang), "JOB_NOT_CANCELLABLE"));
        })
        .RequireFunction("SYS.JOBS")
        .Produces<SuccessResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapGet("/i18n", async (
            int page,
            int pageSize,
            string? key,
            string? culture,
            I18nAdminService i18n,
            ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            if (page < 1 || pageSize < 1 || pageSize > 200)
            {
                var lang = LangHelper.GetLang(http);
                return Results.BadRequest(new ErrorResponse(loc.T("ERR_INVALID_PAGINATION", lang), "INVALID_PAGINATION"));
            }

            var result = await i18n.SearchAsync(page, pageSize, key, culture, ct);
            return Results.Ok(result);
        })
        .RequireFunction("SYS.I18N")
        .Produces<PagedResponse<I18nResourceEntryDto>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/i18n", async (
            SaveI18nResourceRequest request,
            I18nAdminService i18n,
            ILocalization loc,
            HttpContext http,
            CancellationToken ct) =>
        {
            var lang = LangHelper.GetLang(http);
            // 这里去掉了 try-catch，让 GlobalExceptionHandler 处理 I18nAdminService 可能抛出的异常
            // I18nAdminService 内部应确保在必要时抛出 ValidationException 或 DomainException
            if (I18nAdminService.IsProtectedKey(request.Key) && !request.Force)
            {
                // 这个逻辑保留在 Endpoint 还是下沉到 Service? 
                // 最好是下沉。假设 Service 已经有检查。如果没有，这里先保留检查。
                // 如果 Service 抛出 "PROTECTED_KEY"，GlobalHandler 会捕获。
                // 但这里原代码不仅检查还可能抛出异常。
                // 如果 Service.SaveAsync 做了检查并抛出异常，这里就不需要重复检查了。
                // 假设 Service.SaveAsync 已经很健壮。
            }
            
            // 为了保持行为一致，如果 Service 抛出 InvalidOperationException("PROTECTED_KEY")，
            // GlobalExceptionHandler 会捕获它并返回 400。
            // 但我们需要确保 ErrorCode 也是 I18N_KEY_PROTECTED。
            // 暂时保留显式检查逻辑，但去掉 try-catch 包装。
            
            if (I18nAdminService.IsProtectedKey(request.Key) && !request.Force)
            {
                 return Results.BadRequest(new ErrorResponse(loc.T("ERR_I18N_KEY_PROTECTED", lang), "I18N_KEY_PROTECTED"));
            }

            await i18n.SaveAsync(request, ct);
            return Results.Ok(new SuccessResponse(loc.T("MSG_SAVED", lang)));
        })
        .RequireFunction("SYS.I18N")
        .Produces<SuccessResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        group.MapPost("/i18n/reload", (
            I18nAdminService i18n,
            ILocalization loc,
            HttpContext http) =>
        {
            var lang = LangHelper.GetLang(http);
            i18n.ReloadCache();
            return Results.Ok(new SuccessResponse(loc.T("MSG_RELOADED", lang)));
        })
        .RequireFunction("SYS.I18N")
        .Produces<SuccessResponse>(StatusCodes.Status200OK);

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
