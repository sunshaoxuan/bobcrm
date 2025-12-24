using System.Collections.Generic;
using System.Linq;

namespace BobCrm.Api.Abstractions;

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

    /// <summary>构建成功结果</summary>
    public static DataSourceValidationResult Success() => new() { IsValid = true };

    /// <summary>构建失败结果</summary>
    public static DataSourceValidationResult Failure(params string[] errors) =>
        new() { IsValid = false, Errors = errors.ToList() };
}
