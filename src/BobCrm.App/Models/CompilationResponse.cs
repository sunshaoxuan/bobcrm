namespace BobCrm.App.Models;

/// <summary>
/// 编译响应
/// </summary>
public class CompilationResponse
{
    public bool Success { get; set; }
    public string AssemblyName { get; set; } = string.Empty;
    public List<string> LoadedTypes { get; set; } = new();
    public List<CompilationErrorDto> Errors { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
