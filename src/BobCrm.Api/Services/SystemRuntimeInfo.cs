namespace BobCrm.Api.Services;

/// <summary>
/// 系统运行时信息（进程级别），用于系统诊断接口。
/// </summary>
public sealed class SystemRuntimeInfo
{
    public DateTime StartedAtUtc { get; }

    public SystemRuntimeInfo(DateTime startedAtUtc)
    {
        StartedAtUtc = startedAtUtc;
    }
}

