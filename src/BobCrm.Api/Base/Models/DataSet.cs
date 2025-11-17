using System;
using System.Collections.Generic;

namespace BobCrm.Api.Base.Models;

/// <summary>
/// 数据集定义 - 描述模板控件的数据来源
/// </summary>
public class DataSet
{
    /// <summary>数据集ID</summary>
    public int Id { get; set; }

    /// <summary>数据集代码(业务标识)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>数据集名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 数据集显示名称(多语)
    /// 例如: { "zh-CN": "客户列表", "en-US": "Customer List" }
    /// </summary>
    public Dictionary<string, string?>? DisplayName { get; set; }

    /// <summary>
    /// 描述(多语)
    /// </summary>
    public Dictionary<string, string?>? Description { get; set; }

    /// <summary>
    /// 数据源类型代码(关联 DataSourceTypeEntry.Code)
    /// 例如: "entity", "api", "sql", "view"
    /// </summary>
    public string DataSourceTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// 配置JSON - 根据数据源类型,存储不同的配置
    ///
    /// 示例 (EntityDataSource):
    /// {
    ///   "entityType": "customer",
    ///   "includeRelations": ["localization"],
    ///   "defaultFilter": "..."
    /// }
    ///
    /// 示例 (ApiDataSource):
    /// {
    ///   "endpoint": "/api/reports/sales",
    ///   "method": "POST",
    ///   "headers": {...}
    /// }
    ///
    /// 示例 (SqlDataSource):
    /// {
    ///   "sqlQuery": "SELECT * FROM...",
    ///   "parameters": [...]
    /// }
    /// </summary>
    public string? ConfigJson { get; set; }

    /// <summary>
    /// 字段定义(JSON 数组)
    /// 格式: [
    ///   {
    ///     "name": "code",
    ///     "dataType": "string",
    ///     "displayName": { "zh-CN": "编码", "en-US": "Code" },
    ///     "sortable": true,
    ///     "filterable": true
    ///   }, ...
    /// ]
    /// </summary>
    public string? FieldsJson { get; set; }

    /// <summary>是否支持分页</summary>
    public bool SupportsPaging { get; set; } = true;

    /// <summary>是否支持排序</summary>
    public bool SupportsSorting { get; set; } = true;

    /// <summary>默认排序字段</summary>
    public string? DefaultSortField { get; set; }

    /// <summary>默认排序方向("asc"/"desc")</summary>
    public string DefaultSortDirection { get; set; } = "asc";

    /// <summary>每页默认记录数</summary>
    public int DefaultPageSize { get; set; } = 20;

    /// <summary>关联的查询定义ID(可选)</summary>
    public int? QueryDefinitionId { get; set; }

    /// <summary>关联的查询定义</summary>
    public QueryDefinition? QueryDefinition { get; set; }

    /// <summary>关联的权限过滤ID(可选)</summary>
    public int? PermissionFilterId { get; set; }

    /// <summary>关联的权限过滤</summary>
    public PermissionFilter? PermissionFilter { get; set; }

    /// <summary>是否为系统内置数据集</summary>
    public bool IsSystem { get; set; } = false;

    /// <summary>是否启用</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>创建者ID</summary>
    public string? CreatedBy { get; set; }

    /// <summary>更新时间</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>更新者ID</summary>
    public string? UpdatedBy { get; set; }
}
