namespace BobCrm.Api.Core.DomainCommon;

/// <summary>
/// 领域异常基类
/// 用于表示业务逻辑中预期的错误（如验证失败、状态冲突等）
/// 这些异常通常会被全局异常处理器捕获并转换为 400 Bad Request
/// </summary>
public class DomainException : Exception
{
    public string ErrorCode { get; }
    
    public DomainException(string message, string errorCode = "DOMAIN_ERROR") 
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public DomainException(string message, Exception innerException, string errorCode = "DOMAIN_ERROR") 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
