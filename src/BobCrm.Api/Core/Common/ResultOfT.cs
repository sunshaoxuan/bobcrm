using System;
using System.Collections.Generic;
using System.Linq;

namespace BobCrm.Api.Core.Common;

/// <summary>
/// 操作结果包装器
/// 用于统一的错误处理和结果返回，避免使用异常进行业务流程控制
/// </summary>
/// <typeparam name="T">结果数据类型</typeparam>
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ErrorCode { get; private set; }
    public List<string> Errors { get; private set; } = new();

    private Result() { }

    public static Result<T> Success(T data)
        => new Result<T> { IsSuccess = true, Data = data };

    public static Result<T> Failure(string errorMessage, string? errorCode = null)
        => new Result<T> { IsSuccess = false, ErrorMessage = errorMessage, ErrorCode = errorCode };

    public static Result<T> Failure(List<string> errors, string? errorCode = null)
        => new Result<T>
        {
            IsSuccess = false,
            ErrorMessage = errors.FirstOrDefault(),
            ErrorCode = errorCode,
            Errors = errors
        };

    public static Result<T> FromException(Exception exception, string? errorCode = null)
        => new Result<T>
        {
            IsSuccess = false,
            ErrorMessage = exception.Message,
            ErrorCode = errorCode ?? "INTERNAL_ERROR"
        };
}
