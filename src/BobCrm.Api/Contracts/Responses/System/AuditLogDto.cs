namespace BobCrm.Api.Contracts.Responses.System;

/// <summary>
/// 审计日志展示 DTO（用于系统治理与可观测性）。
/// </summary>
public class AuditLogDto
{
    /// <summary>
    /// 审计日志唯一标识。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 模块标识（如 EntityDefinition、RoleProfile 等）。
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型：C/U/D/P 或业务自定义动作。
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 操作人用户 Id（可选）。
    /// </summary>
    public string? ActorId { get; set; }

    /// <summary>
    /// 操作人显示名称（可选）。
    /// </summary>
    public string? ActorName { get; set; }

    /// <summary>
    /// 发起请求的 IP 地址（可选）。
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 关联对象标识（如实体主键或业务编码，可选）。
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// 操作描述（可选）。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 变更前 JSON 快照（可选）。
    /// </summary>
    public string? BeforeJson { get; set; }

    /// <summary>
    /// 变更后 JSON 快照（可选）。
    /// </summary>
    public string? AfterJson { get; set; }

    /// <summary>
    /// 属性级变更明细（JSON 字符串，可选）。
    /// </summary>
    public string? ChangesJson { get; set; }

    /// <summary>
    /// 操作时间（UTC）。
    /// </summary>
    public DateTime OccurredAt { get; set; }
}

