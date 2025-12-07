namespace BobCrm.Api.Contracts.Responses.Entity;

public class CompileResultDto
{
    public bool Success { get; set; }
    public string? AssemblyName { get; set; }
    public List<string> LoadedTypes { get; set; } = new();
    public int? Count { get; set; }
    public string Message { get; set; } = string.Empty;
    public IEnumerable<CompileErrorDto>? Errors { get; set; }
}

public class CompileErrorDto
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public string? FilePath { get; set; }
}
