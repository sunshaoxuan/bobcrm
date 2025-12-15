namespace BobCrm.Api.Services;

/// <summary>
/// 编译错误
/// </summary>
public class CompilationError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public string? FilePath { get; set; }
}

