namespace BobCrm.Api.Contracts.Responses.System;

/// <summary>
/// 系统运行信息（用于系统健康与诊断）。
/// </summary>
public class SystemInfoDto
{
    /// <summary>
    /// 系统启动时间（UTC）。
    /// </summary>
    public DateTime StartedAtUtc { get; set; }

    /// <summary>
    /// 当前进程工作集内存占用（字节）。
    /// </summary>
    public long WorkingSetBytes { get; set; }

    /// <summary>
    /// 主程序版本号。
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 当前数据库 Provider（如 Npgsql.EntityFrameworkCore.PostgreSQL / Microsoft.EntityFrameworkCore.Sqlite）。
    /// </summary>
    public string DbProvider { get; set; } = string.Empty;
}

