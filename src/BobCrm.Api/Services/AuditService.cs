using BobCrm.Api.Abstractions;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;

namespace BobCrm.Api.Services;

public class AuditService : IAuditService
{
    public Task AttachAsync(AppDbContext db, IReadOnlyCollection<AuditLog> logs, CancellationToken ct = default)
    {
        if (logs.Count == 0)
        {
            return Task.CompletedTask;
        }

        db.AuditLogs.AddRange(logs);
        return Task.CompletedTask;
    }
}
