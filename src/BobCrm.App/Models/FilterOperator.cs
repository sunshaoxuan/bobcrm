namespace BobCrm.App.Models;

/// <summary>
/// 过滤操作符常量
/// </summary>
public static class FilterOperator
{
    public new const string Equals = "equals";
    public const string NotEquals = "notEquals";
    public const string Contains = "contains";
    public const string StartsWith = "startsWith";
    public const string EndsWith = "endsWith";
    public const string GreaterThan = "greaterThan";
    public const string GreaterThanOrEqual = "greaterThanOrEqual";
    public const string LessThan = "lessThan";
    public const string LessThanOrEqual = "lessThanOrEqual";
    public const string In = "in";
    public const string NotIn = "notIn";
    public const string IsNull = "isNull";
    public const string IsNotNull = "isNotNull";
}
