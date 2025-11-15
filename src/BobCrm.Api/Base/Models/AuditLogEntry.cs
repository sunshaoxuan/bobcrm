using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Base.Models;

/// <summary>
/// 审计日志条目，用于记录关键操作
/// </summary>
public class AuditLogEntry
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 日志分类，例如 MENU、ROLE 等
    /// </summary>
    [MaxLength(64)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 操作动作，例如 CREATE、UPDATE、DELETE
    /// </summary>
    [MaxLength(64)]
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// 操作描述
    /// </summary>
    [MaxLength(256)]
    public string? Description { get; set; }

    /// <summary>
    /// 操作人用户 Id
    /// </summary>
    [MaxLength(128)]
    public string? ActorId { get; set; }

    /// <summary>
    /// 操作人显示名称
    /// </summary>
    [MaxLength(128)]
    public string? ActorName { get; set; }

    /// <summary>
    /// 关联对象标识（如 FunctionNode Code）
    /// </summary>
    [MaxLength(128)]
    public string? Target { get; set; }

    /// <summary>
    /// 额外上下文数据（JSON 字符串）
    /// </summary>
    public string? Payload { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
