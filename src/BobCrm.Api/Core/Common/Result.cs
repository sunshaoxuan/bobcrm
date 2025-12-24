using System;
using System.Collections.Generic;
using System.Linq;

namespace BobCrm.Api.Core.Common;

/// <summary>
/// 无返回值的操作结果
/// </summary>
public class Result
{
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ErrorCode { get; private set; }
    public List<string> Errors { get; private set; } = new();

    private Result() { }

    public static Result Success()
        => new Result { IsSuccess = true };

    public static Result Failure(string errorMessage, string? errorCode = null)
        => new Result { IsSuccess = false, ErrorMessage = errorMessage, ErrorCode = errorCode };

    public static Result Failure(List<string> errors, string? errorCode = null)
        => new Result
        {
            IsSuccess = false,
            ErrorMessage = errors.FirstOrDefault(),
            ErrorCode = errorCode,
            Errors = errors
        };

    public static Result FromException(Exception exception, string? errorCode = null)
        => new Result
        {
            IsSuccess = false,
            ErrorMessage = exception.Message,
            ErrorCode = errorCode ?? "INTERNAL_ERROR"
        };
}
