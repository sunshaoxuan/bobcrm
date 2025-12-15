namespace BobCrm.Api.Base.Aggregates;

using System.Collections.Generic;

/// <summary>
/// 验证异常
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// 验证错误列表
    /// </summary>
    public List<ValidationError> Errors { get; }

    public ValidationException(List<ValidationError> errors) : base("验证失败")
    {
        Errors = errors;
    }

    public ValidationException(string message) : base(message)
    {
        Errors = new List<ValidationError>
        {
            new ValidationError("General", message)
        };
    }
}
