namespace BobCrm.Api.Services.DataMigration;

/// <summary>
/// 数据迁移影响分析结果
/// </summary>
public class MigrationImpact
{
    /// <summary>实体名称</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>影响的表名</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>受影响的行数（估算）</summary>
    public long AffectedRows { get; set; }

    /// <summary>变更操作列表</summary>
    public List<MigrationOperation> Operations { get; set; } = new();

    /// <summary>警告信息列表</summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>错误信息列表（阻塞性问题）</summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>是否可以安全迁移</summary>
    public bool IsSafe => Errors.Count == 0;

    /// <summary>风险等级：Low、Medium、High、Critical</summary>
    public string RiskLevel { get; set; } = "Low";
}
