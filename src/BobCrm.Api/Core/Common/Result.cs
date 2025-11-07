namespace BobCrm.Api.Core.Common;

/// <summary>
/// 操作结果包装器
/// 用于统一的错误处理和结果返回，避免使用异常进行业务逻辑控制
/// </summary>
/// <typeparam name="T">结果数据类型</typeparam>
public class Result<T>
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// 结果数据（成功时有值）
    /// </summary>
    public T? Data { get; private set; }

    /// <summary>
    /// 错误消息（失败时有值）
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// 错误代码（可选，用于前端国际化）
    /// </summary>
    public string? ErrorCode { get; private set; }

    /// <summary>
    /// 详细错误列表（用于表单验证等场景）
    /// </summary>
    public List<string> Errors { get; private set; } = new();

    /// <summary>
    /// 私有构造函数，通过静态工厂方法创建实例
    /// </summary>
    private Result() { }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static Result<T> Success(T data)
    {
        return new Result<T>
        {
            IsSuccess = true,
            Data = data
        };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static Result<T> Failure(string errorMessage, string? errorCode = null)
    {
        return new Result<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }

    /// <summary>
    /// 创建失败结果（多个错误）
    /// </summary>
    public static Result<T> Failure(List<string> errors, string? errorCode = null)
    {
        return new Result<T>
        {
            IsSuccess = false,
            ErrorMessage = errors.FirstOrDefault(),
            ErrorCode = errorCode,
            Errors = errors
        };
    }

    /// <summary>
    /// 创建失败结果（从异常）
    /// </summary>
    public static Result<T> FromException(Exception exception, string? errorCode = null)
    {
        return new Result<T>
        {
            IsSuccess = false,
            ErrorMessage = exception.Message,
            ErrorCode = errorCode ?? "INTERNAL_ERROR"
        };
    }
}

/// <summary>
/// 无返回值的操作结果
/// </summary>
public class Result
{
    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// 错误消息（失败时有值）
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// 错误代码（可选，用于前端国际化）
    /// </summary>
    public string? ErrorCode { get; private set; }

    /// <summary>
    /// 详细错误列表
    /// </summary>
    public List<string> Errors { get; private set; } = new();

    /// <summary>
    /// 私有构造函数
    /// </summary>
    private Result() { }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static Result Success()
    {
        return new Result { IsSuccess = true };
    }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static Result Failure(string errorMessage, string? errorCode = null)
    {
        return new Result
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }

    /// <summary>
    /// 创建失败结果（多个错误）
    /// </summary>
    public static Result Failure(List<string> errors, string? errorCode = null)
    {
        return new Result
        {
            IsSuccess = false,
            ErrorMessage = errors.FirstOrDefault(),
            ErrorCode = errorCode,
            Errors = errors
        };
    }

    /// <summary>
    /// 创建失败结果（从异常）
    /// </summary>
    public static Result FromException(Exception exception, string? errorCode = null)
    {
        return new Result
        {
            IsSuccess = false,
            ErrorMessage = exception.Message,
            ErrorCode = errorCode ?? "INTERNAL_ERROR"
        };
    }
}
