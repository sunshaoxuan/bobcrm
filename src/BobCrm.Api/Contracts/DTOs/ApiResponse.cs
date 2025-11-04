namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 统一 API 响应模型
/// </summary>
public record ApiResponse<T>(bool Success, T? Data, string? Message = null, ErrorDetail? Error = null);

/// <summary>
/// 简化成功响应
/// </summary>
public record ApiResponse(bool Success, string? Message = null);

/// <summary>
/// 错误详情
/// </summary>
public record ErrorDetail(string Code, string Message, Dictionary<string, string[]>? ValidationErrors = null);

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

