using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace BobCrm.Api.Filters;

/// <summary>
/// 全局异常过滤器
/// 统一处理未捕获的异常，返回标准化的错误响应
/// </summary>
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionFilter(
        ILogger<GlobalExceptionFilter> logger,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;
        var statusCode = GetStatusCode(exception);
        var errorCode = GetErrorCode(exception);

        _logger.LogError(
            exception,
            "[GlobalExceptionFilter] Unhandled exception: {Message}, Type: {Type}, StatusCode: {StatusCode}",
            exception.Message,
            exception.GetType().Name,
            statusCode);

        var response = new
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = GetErrorMessage(exception),
            Details = _environment.IsDevelopment() ? exception.ToString() : null,
            TraceId = context.HttpContext.TraceIdentifier
        };

        context.Result = new ObjectResult(response)
        {
            StatusCode = statusCode
        };

        context.ExceptionHandled = true;
    }

    /// <summary>
    /// 根据异常类型确定HTTP状态码
    /// </summary>
    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException => (int)HttpStatusCode.BadRequest,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            InvalidOperationException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }

    /// <summary>
    /// 根据异常类型确定错误代码
    /// </summary>
    private static string GetErrorCode(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException => "NULL_ARGUMENT",
            ArgumentException => "INVALID_ARGUMENT",
            InvalidOperationException => "INVALID_OPERATION",
            UnauthorizedAccessException => "UNAUTHORIZED",
            KeyNotFoundException => "NOT_FOUND",
            _ => "INTERNAL_ERROR"
        };
    }

    /// <summary>
    /// 获取用户友好的错误消息
    /// </summary>
    private string GetErrorMessage(Exception exception)
    {
        // 在生产环境中，可以从异常消息映射到用户友好的消息
        if (_environment.IsProduction())
        {
            return exception switch
            {
                ArgumentNullException => "必填参数缺失",
                ArgumentException => "请求参数无效",
                InvalidOperationException => "操作无效",
                UnauthorizedAccessException => "未授权访问",
                KeyNotFoundException => "请求的资源不存在",
                _ => "服务器内部错误，请稍后重试"
            };
        }

        return exception.Message;
    }
}
