using System;
using System.Collections.Generic;

namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 数据集查询响应
/// </summary>
public sealed record DataSetDto
{
    public int Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public Dictionary<string, string?>? DisplayName { get; init; }
    public Dictionary<string, string?>? Description { get; init; }
    public required string DataSourceTypeCode { get; init; }
    public string? ConfigJson { get; init; }
    public string? FieldsJson { get; init; }
    public bool SupportsPaging { get; init; }
    public bool SupportsSorting { get; init; }
    public string? DefaultSortField { get; init; }
    public string DefaultSortDirection { get; init; } = "asc";
    public int DefaultPageSize { get; init; }
    public int? QueryDefinitionId { get; init; }
    public string? QueryDefinitionCode { get; init; }
    public int? PermissionFilterId { get; init; }
    public string? PermissionFilterCode { get; init; }
    public bool IsSystem { get; init; }
    public bool IsEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? UpdatedBy { get; init; }
}

/// <summary>
/// 创建数据集请求
/// </summary>
public sealed record CreateDataSetRequest
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public Dictionary<string, string?>? DisplayName { get; init; }
    public Dictionary<string, string?>? Description { get; init; }
    public required string DataSourceTypeCode { get; init; }
    public string? ConfigJson { get; init; }
    public string? FieldsJson { get; init; }
    public bool SupportsPaging { get; init; } = true;
    public bool SupportsSorting { get; init; } = true;
    public string? DefaultSortField { get; init; }
    public string DefaultSortDirection { get; init; } = "asc";
    public int DefaultPageSize { get; init; } = 20;
    public int? QueryDefinitionId { get; init; }
    public int? PermissionFilterId { get; init; }
    public bool IsSystem { get; init; } = false;
    public bool IsEnabled { get; init; } = true;
    public string? CreatedBy { get; init; }
}

/// <summary>
/// 更新数据集请求
/// </summary>
public sealed record UpdateDataSetRequest
{
    public required string Name { get; init; }
    public Dictionary<string, string?>? DisplayName { get; init; }
    public Dictionary<string, string?>? Description { get; init; }
    public string? ConfigJson { get; init; }
    public string? FieldsJson { get; init; }
    public bool SupportsPaging { get; init; } = true;
    public bool SupportsSorting { get; init; } = true;
    public string? DefaultSortField { get; init; }
    public string DefaultSortDirection { get; init; } = "asc";
    public int DefaultPageSize { get; init; } = 20;
    public int? QueryDefinitionId { get; init; }
    public int? PermissionFilterId { get; init; }
    public bool IsEnabled { get; init; } = true;
    public string? UpdatedBy { get; init; }
}

/// <summary>
/// 查询定义 DTO
/// </summary>
public sealed record QueryDefinitionDto
{
    public int Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public Dictionary<string, string?>? DisplayName { get; init; }
    public Dictionary<string, string?>? Description { get; init; }
    public string? ConditionsJson { get; init; }
    public string? ParametersJson { get; init; }
    public string? AggregationsJson { get; init; }
    public string? GroupByFields { get; init; }
    public bool IsEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// 创建查询定义请求
/// </summary>
public sealed record CreateQueryDefinitionRequest
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public Dictionary<string, string?>? DisplayName { get; init; }
    public Dictionary<string, string?>? Description { get; init; }
    public string? ConditionsJson { get; init; }
    public string? ParametersJson { get; init; }
    public string? AggregationsJson { get; init; }
    public string? GroupByFields { get; init; }
}

/// <summary>
/// 更新查询定义请求
/// </summary>
public sealed record UpdateQueryDefinitionRequest
{
    public string? Name { get; init; }
    public Dictionary<string, string?>? DisplayName { get; init; }
    public Dictionary<string, string?>? Description { get; init; }
    public string? ConditionsJson { get; init; }
    public string? ParametersJson { get; init; }
    public string? AggregationsJson { get; init; }
    public string? GroupByFields { get; init; }
    public bool? IsEnabled { get; init; }
}

/// <summary>
/// 权限过滤器 DTO
/// </summary>
public sealed record PermissionFilterDto
{
    public int Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public Dictionary<string, string?>? DisplayName { get; init; }
    public Dictionary<string, string?>? Description { get; init; }
    public string? RequiredFunctionCode { get; init; }
    public string? DataScopeTag { get; init; }
    public string? EntityType { get; init; }
    public string? FilterRulesJson { get; init; }
    public bool EnableFieldLevelPermissions { get; init; }
    public bool IsSystem { get; init; }
    public bool IsEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// 创建权限过滤器请求
/// </summary>
public sealed record CreatePermissionFilterRequest
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public Dictionary<string, string?>? DisplayName { get; init; }
    public Dictionary<string, string?>? Description { get; init; }
    public string? RequiredFunctionCode { get; init; }
    public string? DataScopeTag { get; init; }
    public string? EntityType { get; init; }
    public string? FilterRulesJson { get; init; }
    public bool EnableFieldLevelPermissions { get; init; } = false;
}

/// <summary>
/// 更新权限过滤器请求
/// </summary>
public sealed record UpdatePermissionFilterRequest
{
    public string? Name { get; init; }
    public Dictionary<string, string?>? DisplayName { get; init; }
    public Dictionary<string, string?>? Description { get; init; }
    public string? RequiredFunctionCode { get; init; }
    public string? DataScopeTag { get; init; }
    public string? EntityType { get; init; }
    public string? FilterRulesJson { get; init; }
    public bool? EnableFieldLevelPermissions { get; init; }
    public bool? IsEnabled { get; init; }
}

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

/// <summary>
/// 数据源类型 DTO
/// </summary>
public sealed record DataSourceTypeDto
{
    public Guid Id { get; init; }
    public required string Code { get; init; }
    public Dictionary<string, string?>? DisplayName { get; init; }
    public Dictionary<string, string?>? Description { get; init; }
    public required string HandlerType { get; init; }
    public string? ConfigSchema { get; init; }
    public string Category { get; init; } = "General";
    public string? Icon { get; init; }
    public bool IsSystem { get; init; }
    public bool IsEnabled { get; init; }
    public int SortOrder { get; init; }
}
