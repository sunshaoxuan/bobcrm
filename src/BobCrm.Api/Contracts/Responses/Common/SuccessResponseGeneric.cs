namespace BobCrm.Api.Contracts;

/// <summary>
/// 带数据的成功响应
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
