namespace BobCrm.App.Models;

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
