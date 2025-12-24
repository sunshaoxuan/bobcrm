namespace BobCrm.Api.Contracts.Responses.Entity;

/// <summary>
/// 实体引用检查响应 DTO。
/// 用于描述某实体是否被其它资源引用，以及引用详情。
/// </summary>
public class EntityReferenceCheckResponse
{
    /// <summary>
    /// 是否存在引用。
    /// </summary>
    public bool IsReferenced { get; set; }

    /// <summary>
    /// 引用数量。
    /// </summary>
    public int ReferenceCount { get; set; }

    /// <summary>
    /// 引用详情。
    /// </summary>
    public ReferenceDetailsDto Details { get; set; } = new();
}

