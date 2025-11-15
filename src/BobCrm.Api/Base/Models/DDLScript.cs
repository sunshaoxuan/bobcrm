using System.ComponentModel.DataAnnotations;

namespace BobCrm.Api.Base.Models;

/// <summary>
/// DDL脚本 - 记录实体发布时生成和执行的DDL语句
/// </summary>
public class DDLScript
{
    /// <summary>
    /// DDL脚本ID
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// 所属实体定义ID
    /// </summary>
    [Required]
    public Guid EntityDefinitionId { get; set; }

    /// <summary>
    /// 脚本类型（Create、Alter、Drop）
    /// </summary>
    [Required, MaxLength(50)]
    public string ScriptType { get; set; } = string.Empty;

    /// <summary>
    /// SQL脚本内容
    /// </summary>
    [Required]
    public string SqlScript { get; set; } = string.Empty;

    /// <summary>
    /// 执行状态（Pending、Executed、Failed）
    /// </summary>
    [Required, MaxLength(50)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 执行时间
    /// </summary>
    public DateTime? ExecutedAt { get; set; }

    /// <summary>
    /// 错误消息（执行失败时填充）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 创建人
    /// </summary>
    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 所属实体定义（导航属性）
    /// </summary>
    public EntityDefinition? EntityDefinition { get; set; }
}

/// <summary>
/// DDL脚本类型枚举
/// </summary>
public static class DDLScriptType
{
    /// <summary>
    /// 创建表
    /// </summary>
    public const string Create = "Create";

    /// <summary>
    /// 修改表
    /// </summary>
    public const string Alter = "Alter";

    /// <summary>
    /// 删除表
    /// </summary>
    public const string Drop = "Drop";

    /// <summary>
    /// 创建索引
    /// </summary>
    public const string CreateIndex = "CreateIndex";

    /// <summary>
    /// 删除索引
    /// </summary>
    public const string DropIndex = "DropIndex";
}

/// <summary>
/// DDL脚本状态枚举
/// </summary>
public static class DDLScriptStatus
{
    /// <summary>
    /// 待执行
    /// </summary>
    public const string Pending = "Pending";

    /// <summary>
    /// 已执行
    /// </summary>
    public const string Executed = "Executed";

    /// <summary>
    /// 执行失败
    /// </summary>
    public const string Failed = "Failed";
}
