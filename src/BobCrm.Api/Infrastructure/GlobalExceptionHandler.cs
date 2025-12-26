using Microsoft.AspNetCore.Diagnostics;
using BobCrm.Api.Contracts;
using BobCrm.Api.Core.DomainCommon;
using BobCrm.Api.Base.Aggregates;
using BobCrm.Api.Services;
using BobCrm.Api.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Infrastructure;

/// <summary>
/// 全局异常处理器（ASP.NET Core 8 <c>IExceptionHandler</c>）。
/// 统一输出 <c>ErrorResponse(Code/Message/TraceId/Timestamp)</c>，并尽量通过 <c>ERR_{CODE}</c> 做本地化。
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

        // 记录日志：5xx 视为未处理异常，4xx 视为“预期的业务/校验异常”
        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "[GlobalEx] Unhandled exception path={Path}: {Message}",
                httpContext.Request.Path,
                exception.Message);
        }
        else
        {
            _logger.LogWarning(
                "[GlobalEx] Handled expected exception path={Path}: {Message} ({Code})",
                httpContext.Request.Path,
                exception.Message,
                errorCode);
        }

        // 获取本地化服务（从 RequestServices 获取以保持 scope）
        var loc = httpContext.RequestServices.GetService<ILocalization>();
        var lang = LangHelper.GetLang(httpContext);

        string? message = null;
        if (loc != null && !string.IsNullOrWhiteSpace(msgKey))
        {
            var translated = loc.T(msgKey, lang);
            if (!string.IsNullOrWhiteSpace(translated) &&
                !string.Equals(translated, msgKey, StringComparison.OrdinalIgnoreCase))
            {
                message = translated;
            }
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            // Fallback：生产环境的 5xx 不泄漏内部细节，其余情况可返回异常消息
            message = (_env.IsProduction() && statusCode >= 500) ? "An error occurred." : exception.Message;
        }

        if (_env.IsDevelopment() && statusCode >= 500)
        {
            message = $"{message} (Dev: {exception.Message})";
        }

        var response = new ErrorResponse(message, errorCode)
        {
            TraceId = httpContext.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        if (_env.IsDevelopment())
        {
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
            BobCrm.Api.Core.DomainCommon.DomainException domainEx
                => (StatusCodes.Status400BadRequest, domainEx.ErrorCode, $"ERR_{domainEx.ErrorCode}"),

            BobCrm.Api.Base.Aggregates.DomainException legacyEx
                => (StatusCodes.Status400BadRequest, ErrorCodes.DomainError, legacyEx.MessageKey ?? $"ERR_{ErrorCodes.DomainError}"),

            ServiceException serviceEx => serviceEx.ErrorCode == ErrorCodes.EntityExists
                ? (StatusCodes.Status409Conflict, serviceEx.ErrorCode, $"ERR_{serviceEx.ErrorCode}")
                : (StatusCodes.Status400BadRequest, serviceEx.ErrorCode, $"ERR_{serviceEx.ErrorCode}"),

            ValidationException
                => (StatusCodes.Status400BadRequest, ErrorCodes.ValidationFailed, $"ERR_{ErrorCodes.ValidationFailed}"),

            KeyNotFoundException
                => (StatusCodes.Status404NotFound, ErrorCodes.NotFound, $"ERR_{ErrorCodes.NotFound}"),

            UnauthorizedAccessException
                => (StatusCodes.Status401Unauthorized, ErrorCodes.Unauthorized, $"ERR_{ErrorCodes.Unauthorized}"),

            ArgumentException
                => (StatusCodes.Status400BadRequest, ErrorCodes.InvalidArgument, $"ERR_{ErrorCodes.InvalidArgument}"),

            InvalidOperationException
                => (StatusCodes.Status400BadRequest, ErrorCodes.InvalidOperation, $"ERR_{ErrorCodes.InvalidOperation}"),

            DbUpdateConcurrencyException
                => (StatusCodes.Status409Conflict, ErrorCodes.ConcurrencyConflict, $"ERR_{ErrorCodes.ConcurrencyConflict}"),

            OperationCanceledException
                => (499, ErrorCodes.RequestCancelled, $"ERR_{ErrorCodes.RequestCancelled}"), // Client Closed Request

            _ => (StatusCodes.Status500InternalServerError, ErrorCodes.InternalError, $"ERR_{ErrorCodes.InternalError}")
        };
    }
}
