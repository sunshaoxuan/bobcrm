namespace BobCrm.App.Models;

/// <summary>
/// 编译错误DTO
/// </summary>
public class CompilationErrorDto
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public string? FilePath { get; set; }
}
