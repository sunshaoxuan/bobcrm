namespace BobCrm.Api.Contracts.Responses.DynamicEntity;

/// <summary>
/// 原始表查询结果 DTO。
/// </summary>
public class DynamicEntityRawQueryResultDto
{
    /// <summary>
    /// 查询结果数据行集合。
    /// </summary>
    public List<Dictionary<string, object?>> Data { get; set; } = new();

    /// <summary>
    /// 返回的数据行数。
    /// </summary>
    public int Count { get; set; }
}

