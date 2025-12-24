using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BobCrm.Api.Services.HealthChecks;

/// <summary>
/// 磁盘空间健康检查（检测当前内容根目录所在磁盘剩余空间）。
/// </summary>
public sealed class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _configuration;

    public DiskSpaceHealthCheck(IWebHostEnvironment env, IConfiguration configuration)
    {
        _env = env;
        _configuration = configuration;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var minFreeBytes = _configuration.GetValue<long?>("HealthChecks:Disk:MinFreeBytes") ?? 512L * 1024 * 1024;
        var root = Path.GetPathRoot(_env.ContentRootPath);
        if (string.IsNullOrWhiteSpace(root))
        {
            return Task.FromResult(HealthCheckResult.Degraded("Unable to resolve disk root"));
        }

        var drive = new DriveInfo(root);
        if (!drive.IsReady)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Disk is not ready"));
        }

        var freeBytes = drive.AvailableFreeSpace;
        return freeBytes >= minFreeBytes
            ? Task.FromResult(HealthCheckResult.Healthy($"FreeBytes={freeBytes}"))
            : Task.FromResult(HealthCheckResult.Unhealthy($"Low disk space. FreeBytes={freeBytes}, MinFreeBytes={minFreeBytes}"));
    }
}

