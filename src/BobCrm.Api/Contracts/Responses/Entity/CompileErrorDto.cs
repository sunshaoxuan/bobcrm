namespace BobCrm.Api.Contracts.Responses.Entity;

/// <summary>
/// 编译错误 DTO
/// </summary>
public class CompileErrorDto
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public string? FilePath { get; set; }
}
