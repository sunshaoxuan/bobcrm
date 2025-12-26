using Microsoft.AspNetCore.Diagnostics;
using BobCrm.Api.Contracts;
using BobCrm.Api.Core.DomainCommon;
using BobCrm.Api.Base.Aggregates;
using BobCrm.Api.Services;
using BobCrm.Api.Abstractions;
using System.Net;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Infrastructure;

/// <summary>
/// 全局异常处理器 (ASP.NET Core 8 IExceptionHandler 实现)
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, errorCode, msgKey) = MapException(exception);
        
        // 记录日志 (对于预期的业务异常使用 Warning，意外错误使用 Error)
        if (statusCode >= 500)
        {
            _logger.LogError(exception, "[GlobalEx] Unhandled exception path={Path}: {Message}", httpContext.Request.Path, exception.Message);
        }
        else
        {
            _logger.LogWarning("[GlobalEx] Handled expected exception path={Path}: {Message} ({Code})", httpContext.Request.Path, exception.Message, errorCode);
        }

        // 获取本地化服务 (从 RequestServices 获取以保持 Scope)
        // 注意：如果在 DI 容器释放后发生异常，这可能会失败，所以需要 try-catch 或空检查，
        // 但 IExceptionHandler 通常在请求范围内运行。
        var loc = httpContext.RequestServices.GetService<ILocalization>();
        var lang = LangHelper.GetLang(httpContext);

        string message;
        if (loc != null && !string.IsNullOrEmpty(msgKey))
        {
            // 尝试翻译
            message = loc.T(msgKey, lang);
            
            // 如果翻译结果就是 key 本身（或者 loc.T 没找到），且主要异常是 DomainException，
            // 则可能直接使用 Exception.Message (如果它不是 key 的话)
            // 这里约定：DomainException 的 Message 通常是英文技术描述，或者 ErrorCode 对应的 Key
            // 我们优先使用 Key 做翻译。
            
            // 修正策略：
            // 1. 如果有 msgKey，尝试 loc.T(msgKey)
            // 2. 如果是 Dev 环境且 loc 返回了 key (翻译未命中)，或者 statusCode 500，追加 ex.Message
            
            // 简单策略：
            if (_env.IsDevelopment() && statusCode == 500)
            {
                message = $"{message} (Dev: {exception.Message})";
            }
        }
        else
        {
             // Fallback
             message = _env.IsProduction() ? "An error occurred." : exception.Message;
        }

        // 如果是 DomainException 且 Message 看起来是具体的错误信息而非 Key，
        // 在没有特定 MsgKey 映射的情况下，我们可以考虑直接透传 Message (慎用，取决于约定)
        // 但为了 P1-001 去中文化，我们尽量依赖 ErrorCode 映射出的 MsgKey。
        // 下面的 MapException 返回了默认的 MsgKey。

        var response = new ErrorResponse(message, errorCode)
        {
            TraceId = httpContext.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        if (_env.IsDevelopment())
        {
            // 开发环境附加详情
            response.Details = new Dictionary<string, string[]>
            {
                ["stackTrace"] = new[] { exception.StackTrace ?? string.Empty },
                ["exceptionType"] = new[] { exception.GetType().FullName ?? string.Empty },
                ["rawMessage"] = new[] { exception.Message }
            };
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
    
    private static (int StatusCode, string ErrorCode, string? DefaultMsgKey) MapException(Exception ex)
    {
        return ex switch
        {
            BobCrm.Api.Core.DomainCommon.DomainException domainEx => (StatusCodes.Status400BadRequest, domainEx.ErrorCode, $"ERR_{domainEx.ErrorCode}"),
            BobCrm.Api.Base.Aggregates.DomainException legacyEx => (StatusCodes.Status400BadRequest, "DOMAIN_ERROR", legacyEx.MessageKey ?? "ERR_DOMAIN_ERROR"),
            ServiceException serviceEx => serviceEx.ErrorCode == "ENTITY_EXISTS" 
                ? (StatusCodes.Status409Conflict, serviceEx.ErrorCode, $"ERR_{serviceEx.ErrorCode}") 
                : (StatusCodes.Status400BadRequest, serviceEx.ErrorCode, $"ERR_{serviceEx.ErrorCode}"),
            ValidationException valEx => (StatusCodes.Status400BadRequest, "VALIDATION_ERROR", "ERR_VALIDATION_FAILED"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "NOT_FOUND", "ERR_RESOURCE_NOT_FOUND"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "ERR_UNAUTHORIZED"),
            ArgumentException argEx => (StatusCodes.Status400BadRequest, "INVALID_ARGUMENT", "ERR_INVALID_ARGUMENT"),
            InvalidOperationException invOp => (StatusCodes.Status400BadRequest, "INVALID_OPERATION", "ERR_INVALID_OPERATION"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "CONCURRENCY_CONFLICT", "ERR_CONCURRENCY_CONFLICT"),
            OperationCanceledException => (499, "CANCELLED", "ERR_REQUEST_CANCELLED"), // Client Closed Request
            _ => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "ERR_INTERNAL_SERVER_ERROR")
        };
    }
}
