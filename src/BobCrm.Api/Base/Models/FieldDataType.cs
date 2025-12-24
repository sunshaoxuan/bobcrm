namespace BobCrm.Api.Base.Models;

/// <summary>
/// 数据类型枚举
/// </summary>
public static class FieldDataType
{
    public const string String = "String";
    public const string Int32 = "Int32";
    public const string Int64 = "Int64";
    public const string Decimal = "Decimal";
    public const string DateTime = "DateTime";
    public const string Date = "Date";  // 独立的日期类型（仅日期，不含时间）
    public const string Boolean = "Boolean";
    public const string Guid = "Guid";
    public const string EntityRef = "EntityRef";  // 子实体引用
    public const string Enum = "Enum";  // 动态枚举类型

    // 别名常量，用于代码中更直观的引用
    public const string Integer = Int32;
    public const string Long = Int64;
    public const string Text = String;
}

