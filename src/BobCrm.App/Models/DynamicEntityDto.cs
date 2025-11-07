namespace BobCrm.App.Models;

/// <summary>
/// 动态实体查询请求
/// </summary>
public class DynamicEntityQueryRequest
{
    public List<FilterConditionDto>? Filters { get; set; }
    public string? OrderBy { get; set; }
    public bool OrderByDescending { get; set; }
    public int? Skip { get; set; }
    public int? Take { get; set; }
}

/// <summary>
/// 过滤条件DTO
/// </summary>
public class FilterConditionDto
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = "equals";
    public object Value { get; set; } = string.Empty;
}

/// <summary>
/// 动态实体查询响应
/// </summary>
public class DynamicEntityQueryResponse
{
    public List<Dictionary<string, object>> Data { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

/// <summary>
/// 动态实体统计请求
/// </summary>
public class DynamicEntityCountRequest
{
    public List<FilterConditionDto>? Filters { get; set; }
}

/// <summary>
/// 动态实体统计响应
/// </summary>
public class DynamicEntityCountResponse
{
    public int Count { get; set; }
}

/// <summary>
/// 过滤操作符常量
/// </summary>
public static class FilterOperator
{
    public const string Equals = "equals";
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
