using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.Responses.System;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

public sealed class AuditLogService
{
    private readonly AppDbContext _db;

    public AuditLogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResponse<AuditLogDto>> SearchAsync(
        int page,
        int pageSize,
        string? module,
        string? operationType,
        string? actorQuery,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken ct)
    {
        var query = _db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(x => x.Module == module);
        }

        if (!string.IsNullOrWhiteSpace(operationType))
        {
            query = query.Where(x => x.OperationType == operationType);
        }

        if (!string.IsNullOrWhiteSpace(actorQuery))
        {
            var q = actorQuery.Trim();
            query = query.Where(x =>
                (x.ActorId != null && x.ActorId.Contains(q))
                || (x.ActorName != null && x.ActorName.Contains(q)));
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

        return new PagedResponse<AuditLogDto>(items, page, pageSize, totalCount);
    }

    public async Task<List<string>> GetModulesAsync(int limit, CancellationToken ct)
    {
        limit = Math.Clamp(limit, 1, 200);

        return await _db.AuditLogs.AsNoTracking()
            .Where(x => x.Module != null && x.Module != "")
            .Select(x => x.Module)
            .Distinct()
            .OrderBy(x => x)
            .Take(limit)
            .ToListAsync(ct);
    }
}

