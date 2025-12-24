namespace BobCrm.App.Models;

/// <summary>
/// 动态实体统计请求
/// </summary>
public class DynamicEntityCountRequest
{
    public List<FilterConditionDto>? Filters { get; set; }
}
