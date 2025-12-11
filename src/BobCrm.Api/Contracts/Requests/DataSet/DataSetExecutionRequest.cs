namespace BobCrm.Api.Contracts.Requests.DataSet;

/// <summary>
/// 数据集执行请求(用于运行态获取数据)
/// </summary>
public sealed record DataSetExecutionRequest
{
    /// <summary>数据集ID</summary>
    public int DataSetId { get; init; }

    /// <summary>当前页(从1开始)</summary>
    public int Page { get; init; } = 1;

    /// <summary>每页记录数(null则使用数据集默认值)</summary>
    public int? PageSize { get; init; }

    /// <summary>排序字段</summary>
    public string? SortField { get; init; }

    /// <summary>排序方向(asc/desc)</summary>
    public string? SortDirection { get; init; }

    /// <summary>
    /// 运行时参数(JSON 对象)
    /// 例如: { "searchText": "customer", "status": "active" }
    /// </summary>
    public string? RuntimeParametersJson { get; init; }

    /// <summary>运行时上下文(当前用户、组织等)</summary>
    public BobCrm.Api.Abstractions.DataSourceRuntimeContext? RuntimeContext { get; init; }
}
