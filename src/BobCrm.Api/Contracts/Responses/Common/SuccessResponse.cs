namespace BobCrm.Api.Contracts;

/// <summary>
/// 无数据的成功响应
/// </summary>
public class SuccessResponse : BaseResponse
{
    public override bool Success => true;
    
    /// <summary>
    /// 成功消息
    /// </summary>
    public string? Message { get; set; }

    public SuccessResponse(string? message = null)
    {
        Message = message;
    }
}
