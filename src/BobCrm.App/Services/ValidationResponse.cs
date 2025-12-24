namespace BobCrm.App.Services;

/// <summary>
/// 验证响应
/// </summary>
public class ValidationResponse
{
    public bool IsValid { get; set; }
    public List<BobCrm.App.Models.CompilationErrorDto> Errors { get; set; } = new();
}
