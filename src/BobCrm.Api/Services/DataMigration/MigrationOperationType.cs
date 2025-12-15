namespace BobCrm.Api.Services.DataMigration;

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

