namespace BobCrm.Api.Contracts;

/// <summary>
/// API 响应辅助方法（对接 BaseResponse / SuccessResponse / SuccessResponse&lt;T&gt; / ErrorResponse 体系）
/// </summary>
public static class ApiResponseExtensions
{
    public static SuccessResponse SuccessResponse(string? message = null) =>
        new(message ?? "操作成功");

    public static SuccessResponse<T> SuccessResponse<T>(T data) =>
        new(data);

    public static ErrorResponse ErrorResponse(string code, string message, Dictionary<string, string[]>? validationErrors = null) =>
        validationErrors is { Count: > 0 }
            ? new ErrorResponse(message, validationErrors, code)
            : new ErrorResponse(message, code);
}
