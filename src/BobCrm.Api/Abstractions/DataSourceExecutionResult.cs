namespace BobCrm.Api.Abstractions;

/// <summary>
/// 数据源执行结果
/// </summary>
public record DataSourceExecutionResult
{
    /// <summary>数据行(JSON 数组)</summary>
    public required string DataJson { get; init; }

    /// <summary>总记录数</summary>
    public int TotalCount { get; init; }

    /// <summary>当前页</summary>
    public int Page { get; init; }

    /// <summary>每页记录数</summary>
    public int PageSize { get; init; }

    /// <summary>总页数</summary>
    public int TotalPages { get; init; }

    /// <summary>应用的权限范围描述(用于前端展示)</summary>
    public string[]? AppliedScopes { get; init; }

    /// <summary>执行耗时(毫秒)</summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>是否来自缓存</summary>
    public bool IsFromCache { get; init; } = false;
}
