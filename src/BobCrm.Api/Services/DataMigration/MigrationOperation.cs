namespace BobCrm.Api.Services.DataMigration;

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

