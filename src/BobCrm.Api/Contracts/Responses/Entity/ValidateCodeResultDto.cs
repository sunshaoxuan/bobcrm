namespace BobCrm.Api.Contracts.Responses.Entity;

public class ValidateCodeResultDto
{
    public bool IsValid { get; set; }
    public IEnumerable<CompileErrorDto> Errors { get; set; } = Enumerable.Empty<CompileErrorDto>();
}
