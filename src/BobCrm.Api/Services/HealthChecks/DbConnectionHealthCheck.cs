using BobCrm.Api.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BobCrm.Api.Services.HealthChecks;

/// <summary>
/// 数据库连通性健康检查（支持 SQLite / PostgreSQL）。
/// </summary>
public sealed class DbConnectionHealthCheck : IHealthCheck
{
    private readonly AppDbContext _db;

    public DbConnectionHealthCheck(AppDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_db.Database.ProviderName?.Contains("InMemory", StringComparison.OrdinalIgnoreCase) == true)
        {
            return HealthCheckResult.Healthy("InMemory provider");
        }

        var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
        return canConnect
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Cannot connect to database");
    }
}

