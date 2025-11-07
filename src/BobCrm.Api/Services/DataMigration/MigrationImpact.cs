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

/// <summary>
/// 单个迁移操作
/// </summary>
public class MigrationOperation
{
    /// <summary>操作类型：AddColumn、DropColumn、AlterColumn、RenameColumn</summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>字段名称</summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>旧数据类型（如果是修改）</summary>
    public string? OldDataType { get; set; }

    /// <summary>新数据类型</summary>
    public string? NewDataType { get; set; }

    /// <summary>是否可能导致数据丢失</summary>
    public bool MayLoseData { get; set; }

    /// <summary>需要数据转换</summary>
    public bool RequiresConversion { get; set; }

    /// <summary>操作描述</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>SQL预览</summary>
    public string SqlPreview { get; set; } = string.Empty;
}

/// <summary>
/// 迁移操作类型
/// </summary>
public static class MigrationOperationType
{
    public const string AddColumn = "AddColumn";
    public const string DropColumn = "DropColumn";
    public const string AlterColumn = "AlterColumn";
    public const string RenameColumn = "RenameColumn";
    public const string AddTable = "AddTable";
    public const string DropTable = "DropTable";
    public const string AddForeignKey = "AddForeignKey";
    public const string DropForeignKey = "DropForeignKey";
}

/// <summary>
/// 风险等级
/// </summary>
public static class RiskLevel
{
    /// <summary>低风险：仅添加可空字段</summary>
    public const string Low = "Low";

    /// <summary>中等风险：修改字段类型但可以安全转换</summary>
    public const string Medium = "Medium";

    /// <summary>高风险：可能导致数据丢失</summary>
    public const string High = "High";

    /// <summary>严重风险：必定导致数据丢失或系统异常</summary>
    public const string Critical = "Critical";
}
