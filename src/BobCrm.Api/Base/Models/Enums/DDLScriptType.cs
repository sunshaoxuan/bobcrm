namespace BobCrm.Api.Base.Models;

/// <summary>
/// DDL脚本类型枚举
/// </summary>
public static class DDLScriptType
{
    public const string Create = "Create";
    public const string Alter = "Alter";
    public const string Drop = "Drop";
    public const string Rollback = "Rollback";

    public const string CreateIndex = "CreateIndex";
    public const string DropIndex = "DropIndex";
}

