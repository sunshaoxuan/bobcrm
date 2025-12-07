namespace BobCrm.Api.Contracts;

/// <summary>
/// 错误响应包装类
/// </summary>
public class ErrorResponse : BaseResponse
{
    public override bool Success => false;

    /// <summary>
    /// 错误代码
    /// </summary>
    public string Code { get; set; } = "ERROR";

    /// <summary>
    /// 错误消息
    /// </summary>
    public string Message { get; set; } = "An error occurred";

    /// <summary>
    /// 详细错误信息 (如验证错误)
    /// </summary>
    public Dictionary<string, string[]>? Details { get; set; }

    public ErrorResponse() { }

    public ErrorResponse(string message, string code = "ERROR")
    {
        Message = message;
        Code = code;
    }

    public ErrorResponse(string message, Dictionary<string, string[]> details, string code = "VALIDATION_ERROR")
    {
        Message = message;
        Details = details;
        Code = code;
    }
}
