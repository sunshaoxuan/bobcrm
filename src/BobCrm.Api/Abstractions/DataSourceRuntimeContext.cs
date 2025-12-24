using System;

namespace BobCrm.Api.Abstractions;

/// <summary>
/// 数据源执行时上下文
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
