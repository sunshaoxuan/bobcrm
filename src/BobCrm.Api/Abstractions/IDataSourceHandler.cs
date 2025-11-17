using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BobCrm.Api.Abstractions;

/// <summary>
/// 数据源处理器接口 - 定义数据源执行的统一契约
/// 每种数据源类型(Entity/Api/Sql/View)都需要实现此接口
/// </summary>
public interface IDataSourceHandler
{
    /// <summary>
    /// 数据源类型代码
    /// 对应 DataSourceTypeEntry.Code
    /// </summary>
    string TypeCode { get; }

    /// <summary>
    /// 执行数据源查询
    /// </summary>
    /// <param name="request">执行请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果</returns>
    Task<DataSourceExecutionResult> ExecuteAsync(
        DataSourceExecutionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证数据源配置
    /// </summary>
    /// <param name="configJson">配置JSON</param>
    /// <returns>验证结果</returns>
    Task<DataSourceValidationResult> ValidateConfigAsync(string configJson);

    /// <summary>
    /// 获取字段元数据
    /// 根据数据源配置,推断可用的字段列表
    /// </summary>
    /// <param name="configJson">配置JSON</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字段列表</returns>
    Task<List<DataSourceFieldMetadata>> GetFieldsAsync(
        string configJson,
        CancellationToken cancellationToken = default);
}

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
    /// 运行时参数(JSON 对象)
    /// 例如: { "searchText": "customer", "status": "active" }
    /// </summary>
    public string? RuntimeParametersJson { get; init; }

    /// <summary>
    /// 运行时上下文
    /// 包含当前用户、组织、角色等信息
    /// </summary>
    public DataSourceRuntimeContext? RuntimeContext { get; init; }
}

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

/// <summary>
/// 数据源运行时上下文
/// </summary>
public record DataSourceRuntimeContext
{
    /// <summary>当前用户ID</summary>
    public string? CurrentUserId { get; init; }

    /// <summary>当前组织ID</summary>
    public Guid? CurrentOrganizationId { get; init; }

    /// <summary>当前角色ID</summary>
    public Guid? CurrentRoleId { get; init; }

    /// <summary>当前语言代码</summary>
    public string CurrentLanguage { get; init; } = "zh-CN";

    /// <summary>自定义上下文变量(JSON对象)</summary>
    public string? CustomContextJson { get; init; }
}

/// <summary>
/// 数据源字段元数据
/// </summary>
public record DataSourceFieldMetadata
{
    /// <summary>字段名称</summary>
    public required string Name { get; init; }

    /// <summary>数据类型</summary>
    public required string DataType { get; init; }

    /// <summary>
    /// 显示名称(多语)
    /// 例如: { "zh-CN": "客户编码", "en-US": "Customer Code" }
    /// </summary>
    public Dictionary<string, string?>? DisplayName { get; init; }

    /// <summary>是否可排序</summary>
    public bool Sortable { get; init; } = false;

    /// <summary>是否可筛选</summary>
    public bool Filterable { get; init; } = false;

    /// <summary>是否必填</summary>
    public bool Required { get; init; } = false;
}

/// <summary>
/// 数据源验证结果
/// </summary>
public record DataSourceValidationResult
{
    /// <summary>是否有效</summary>
    public bool IsValid { get; init; }

    /// <summary>错误消息列表</summary>
    public List<string> Errors { get; init; } = new();

    /// <summary>警告消息列表</summary>
    public List<string> Warnings { get; init; } = new();

    /// <summary>创建成功结果</summary>
    public static DataSourceValidationResult Success() => new() { IsValid = true };

    /// <summary>创建失败结果</summary>
    public static DataSourceValidationResult Failure(params string[] errors) =>
        new() { IsValid = false, Errors = errors.ToList() };
}
