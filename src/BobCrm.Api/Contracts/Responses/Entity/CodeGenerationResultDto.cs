namespace BobCrm.Api.Contracts.Responses.Entity;

public class CodeGenerationResultDto
{
    public Guid EntityId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
