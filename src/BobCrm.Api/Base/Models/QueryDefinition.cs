using System;
using System.Collections.Generic;

namespace BobCrm.Api.Base.Models;

/// <summary>
/// 查询定义 - 描述数据集的筛选条件和参数
/// </summary>
public class QueryDefinition
{
    /// <summary>查询定义ID</summary>
    public int Id { get; set; }

    /// <summary>查询代码(业务标识)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>查询名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 查询显示名称(多语)
    /// </summary>
    public Dictionary<string, string?>? DisplayName { get; set; }

    /// <summary>
    /// 描述(多语)
    /// </summary>
    public Dictionary<string, string?>? Description { get; set; }

    /// <summary>
    /// 条件表达式(JSON 格式)
    /// 格式: { "operator": "AND", "conditions": [...] }
    /// 支持的运算符: AND, OR, EQ, NE, GT, LT, GTE, LTE, LIKE, IN, BETWEEN
    ///
    /// 示例:
    /// {
    ///   "operator": "AND",
    ///   "conditions": [
    ///     { "field": "status", "operator": "EQ", "value": "active" },
    ///     { "field": "organizationId", "operator": "EQ", "value": "@CurrentOrganizationId" }
    ///   ]
    /// }
    /// </summary>
    public string? ConditionsJson { get; set; }

    /// <summary>
    /// 参数定义(JSON 数组)
    /// 格式: [
    ///   {
    ///     "name": "CurrentUserId",
    ///     "dataType": "string",
    ///     "source": "context",
    ///     "description": { "zh-CN": "当前用户ID" }
    ///   }, ...
    /// ]
    ///
    /// 支持的参数源:
    /// - context: 从运行时上下文获取(CurrentUserId, CurrentOrganizationId, CurrentRoleId)
    /// - input: 用户输入参数
    /// - constant: 常量值
    /// </summary>
    public string? ParametersJson { get; set; }

    /// <summary>
    /// 聚合字段(可选,JSON 数组)
    /// 格式: [
    ///   {
    ///     "field": "amount",
    ///     "function": "SUM",
    ///     "alias": "totalAmount"
    ///   }, ...
    /// ]
    /// 支持的函数: COUNT, SUM, AVG, MIN, MAX
    /// </summary>
    public string? AggregationsJson { get; set; }

    /// <summary>
    /// 分组字段(可选,逗号分隔)
    /// 例如: "customerId,productId"
    /// </summary>
    public string? GroupByFields { get; set; }

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
