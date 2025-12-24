using BobCrm.Api.Services.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Net.Sockets;

namespace BobCrm.Api.Services.HealthChecks;

/// <summary>
/// S3/MinIO 服务连通性健康检查（仅检测网络连通性，不校验凭据）。
/// </summary>
public sealed class S3ConnectivityHealthCheck : IHealthCheck
{
    private readonly IOptions<S3Options> _options;

    public S3ConnectivityHealthCheck(IOptions<S3Options> options)
    {
        _options = options;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var url = _options.Value.ServiceUrl;
        if (string.IsNullOrWhiteSpace(url))
        {
            return HealthCheckResult.Healthy("S3 not configured");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            if (!Uri.TryCreate($"http://{url}", UriKind.Absolute, out uri))
            {
                return HealthCheckResult.Unhealthy("Invalid S3 ServiceUrl");
            }
        }

        var port = uri.IsDefaultPort
            ? (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ? 443 : 80)
            : uri.Port;

        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(2));
            await client.ConnectAsync(uri.Host, port, cts.Token);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("S3 endpoint not reachable", ex);
        }
    }
}

