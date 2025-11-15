using System.Security.Claims;
using System.Text.Json;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;

namespace BobCrm.Api.Services;

public class AuditTrailService
{
    private static readonly JsonSerializerOptions PayloadSerializer = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditTrailService(AppDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task RecordAsync(
        string category,
        string action,
        string? description,
        string? target,
        object? payload,
        CancellationToken ct = default)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var actorId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        var actorName = user?.Identity?.Name ?? user?.FindFirstValue("name") ?? actorId;

        var entry = new AuditLogEntry
        {
            Category = category,
            Action = action,
            Description = description,
            ActorId = actorId,
            ActorName = actorName,
            Target = target,
            Payload = payload == null ? null : JsonSerializer.Serialize(payload, PayloadSerializer),
            CreatedAt = DateTime.UtcNow
        };

        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}
