using System.Text.Json.Serialization;

namespace BobCrm.Api.Contracts;

/// <summary>
/// 基础 API 响应类
/// </summary>
public abstract class BaseResponse
{
    /// <summary>
    /// API 版本
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// 响应时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 请求 ID (用于追踪)
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// 操作是否成功
    /// </summary>
    public abstract bool Success { get; }
}
