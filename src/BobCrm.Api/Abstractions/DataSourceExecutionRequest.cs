using System;

namespace BobCrm.Api.Abstractions;

/// <summary>
/// 数据源执行请求
/// </summary>
public record DataSourceExecutionRequest
{
    /// <summary>数据源类型代码</summary>
    public required string TypeCode { get; init; }

    /// <summary>配置JSON</summary>
    public required string ConfigJson { get; init; }

    /// <summary>当前页(从1开始)</summary>
    public int Page { get; init; } = 1;

    /// <summary>每页记录数</summary>
    public int PageSize { get; init; } = 20;

    /// <summary>排序字段</summary>
    public string? SortField { get; init; }

    /// <summary>排序方向(asc/desc)</summary>
    public string? SortDirection { get; init; }

    /// <summary>
    /// 执行时参数(JSON 对象)
    /// 例如: { "searchText": "customer", "status": "active" }
    /// </summary>
    public string? RuntimeParametersJson { get; init; }

    /// <summary>
    /// 执行时上下文
    /// 包含当前用户、组织、角色等信息
    /// </summary>
    public DataSourceRuntimeContext? RuntimeContext { get; init; }
}
