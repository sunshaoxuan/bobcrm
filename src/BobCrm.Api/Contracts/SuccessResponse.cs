namespace BobCrm.Api.Contracts;

/// <summary>
/// 成功响应包装类
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class SuccessResponse<T> : BaseResponse
{
    public override bool Success => true;

    /// <summary>
    /// 响应数据
    /// </summary>
    public T? Data { get; set; }

    public SuccessResponse() { }

    public SuccessResponse(T data)
    {
        Data = data;
    }
}

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
