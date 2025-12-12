namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// API 响应辅助方法
/// </summary>
public static class ApiResponseExtensions
{
    public static ApiResponse<T> SuccessResponse<T>(T data, string? message = null) =>
        new(true, data, message);

    public static ApiResponse SuccessResponse(string? message = "操作成功") =>
        new(true, message);

    public static ApiResponse<T> ErrorResponse<T>(string code, string message, Dictionary<string, string[]>? validationErrors = null) =>
        new(false, default, message, new ErrorDetail(code, message, validationErrors));

    public static ApiResponse ErrorResponse(string code, string message, Dictionary<string, string[]>? validationErrors = null) =>
        new(false, message);
}
