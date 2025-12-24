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
