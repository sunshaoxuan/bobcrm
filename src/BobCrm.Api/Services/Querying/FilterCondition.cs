namespace BobCrm.Api.Services;

/// <summary>
/// 过滤条件
/// </summary>
public class FilterCondition
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = "equals"; // equals, contains, greaterThan, lessThan
    public object Value { get; set; } = string.Empty;
}

