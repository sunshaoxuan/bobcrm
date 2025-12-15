namespace BobCrm.Api.Base.Aggregates;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// 验证错误列表
    /// </summary>
    public List<ValidationError> Errors { get; }

    /// <summary>
    /// 是否验证通过
    /// </summary>
    public bool IsValid => !Errors.Any();

    public ValidationResult(List<ValidationError> errors)
    {
        Errors = errors ?? new List<ValidationError>();
    }
}
