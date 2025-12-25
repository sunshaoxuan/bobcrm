using BobCrm.Api.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Sockets;

namespace BobCrm.Api.Services.HealthChecks;

/// <summary>
/// SMTP 服务连通性健康检查（仅检测网络连通性，不校验鉴权）。
/// </summary>
public sealed class SmtpConnectivityHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;

    public SmtpConnectivityHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        string? host = null;
        var port = 25;

        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var settings = await db.SystemSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
            host = settings?.SmtpHost;
            port = settings?.SmtpPort ?? 25;
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            return HealthCheckResult.Healthy("SMTP not configured");
        }

        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(2));
            await client.ConnectAsync(host, port, cts.Token);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SMTP endpoint not reachable", ex);
        }
    }
}
