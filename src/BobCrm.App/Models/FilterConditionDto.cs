namespace BobCrm.App.Models;

/// <summary>
/// 过滤条件DTO
/// </summary>
public class FilterConditionDto
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = "equals";
    public object Value { get; set; } = string.Empty;
}
