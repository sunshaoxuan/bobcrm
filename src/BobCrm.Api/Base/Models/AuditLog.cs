using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Base.Models;

/// <summary>
/// 系统审计日志，用于记录关键操作与实体变更轨迹。
/// </summary>
public class AuditLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 模块标识（如 ENTITY、ROLE、MENU 等）。
    /// </summary>
    [MaxLength(64)]
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型：C（Create）/ U（Update）/ D（Delete）/ P（Publish），或业务自定义动作。
    /// </summary>
    [MaxLength(64)]
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 操作描述（可选）。
    /// </summary>
    [MaxLength(256)]
    public string? Description { get; set; }

    /// <summary>
    /// 操作人用户 Id（可选）。
    /// </summary>
    [MaxLength(128)]
    public string? ActorId { get; set; }

    /// <summary>
    /// 操作人显示名称（可选）。
    /// </summary>
    [MaxLength(128)]
    public string? ActorName { get; set; }

    /// <summary>
    /// 发起请求的 IP 地址（可选）。
    /// </summary>
    [MaxLength(64)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// 关联对象标识（如实体主键或业务编码，可选）。
    /// </summary>
    [MaxLength(128)]
    public string? Target { get; set; }

    /// <summary>
    /// 额外上下文数据（JSON 字符串，可选）。
    /// </summary>
    public string? ContextJson { get; set; }

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
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
