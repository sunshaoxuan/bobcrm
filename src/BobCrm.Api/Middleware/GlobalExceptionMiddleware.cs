using System.Net;
using BobCrm.Api.Contracts;

namespace BobCrm.Api.Middleware;

/// <summary>
/// 全局异常处理中间件
/// 统一捕获和处理未处理的异常，返回标准化的错误响应
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = GetStatusCode(exception);
        var errorCode = GetErrorCode(exception);

        _logger.LogError(
            exception,
            "[GlobalException] Unhandled exception: {Message}, Type: {Type}, StatusCode: {StatusCode}, Path: {Path}",
            exception.Message,
            exception.GetType().Name,
            statusCode,
            context.Request.Path);

        var response = new ErrorResponse(
            _environment.IsDevelopment() ? exception.ToString() : GetErrorMessage(exception),
            errorCode)
        {
            TraceId = context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(response);
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
        // 在生产环境中，返回用户友好的消息
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

/// <summary>
/// 全局异常中间件扩展方法
/// </summary>
public static class GlobalExceptionMiddlewareExtensions
{
    /// <summary>
    /// 使用全局异常处理中间件
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
