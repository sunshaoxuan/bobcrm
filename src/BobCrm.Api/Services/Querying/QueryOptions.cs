namespace BobCrm.Api.Services;

/// <summary>
/// 查询选项
/// </summary>
public class QueryOptions
{
    public List<FilterCondition>? Filters { get; set; }
    public string? OrderBy { get; set; }
    public bool OrderByDescending { get; set; }
    public int? Skip { get; set; }
    public int? Take { get; set; }
}

