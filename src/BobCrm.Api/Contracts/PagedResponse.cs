namespace BobCrm.Api.Contracts;

/// <summary>
/// 分页响应包装类
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class PagedResponse<T> : SuccessResponse<IEnumerable<T>>
{
    /// <summary>
    /// 当前页码 (1-based)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// 每页大小
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总记录数
    /// </summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    public PagedResponse() { }

    public PagedResponse(IEnumerable<T> data, int page, int pageSize, long totalCount) 
        : base(data)
    {
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}
