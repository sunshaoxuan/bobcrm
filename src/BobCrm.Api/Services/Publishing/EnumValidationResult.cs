namespace BobCrm.Api.Services;

/// <summary>
/// 枚举引用验证结果
/// </summary>
public class EnumValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}

