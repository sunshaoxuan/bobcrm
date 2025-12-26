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
    private readonly TimeProvider _timeProvider;

    public AuditTrailService(AppDbContext db, IHttpContextAccessor httpContextAccessor, TimeProvider timeProvider)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _timeProvider = timeProvider;
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
        var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

        var entry = new AuditLog
        {
            Module = category,
            OperationType = action,
            Description = description,
            ActorId = actorId,
            ActorName = actorName,
            IpAddress = ipAddress,
            Target = target,
            ContextJson = payload == null ? null : JsonSerializer.Serialize(payload, PayloadSerializer),
            OccurredAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync(ct);
    }
}
