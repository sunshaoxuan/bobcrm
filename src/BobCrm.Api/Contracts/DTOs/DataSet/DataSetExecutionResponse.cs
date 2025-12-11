namespace BobCrm.Api.Contracts.DTOs.DataSet;

/// <summary>
/// 数据集执行响应
/// </summary>
public sealed record DataSetExecutionResponse
{
    /// <summary>数据集ID</summary>
    public int DataSetId { get; init; }

    /// <summary>数据集Code</summary>
    public required string DataSetCode { get; init; }

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
}
