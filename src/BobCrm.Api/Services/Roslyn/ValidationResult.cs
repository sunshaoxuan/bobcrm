using System.Collections.Generic;

namespace BobCrm.Api.Services;

/// <summary>
/// 语法验证结果
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<CompilationError> Errors { get; set; } = new();
}
