namespace BobCrm.Api.Services;

public class EntityMenuRegistrationResult
{
    public bool Success { get; set; }
    public string DomainCode { get; set; } = string.Empty;
    public Guid? DomainNodeId { get; set; }
    public Guid? ModuleNodeId { get; set; }
    public Guid? FunctionNodeId { get; set; }
    public string? FunctionCode { get; set; }
    public int? TemplateBindingId { get; set; }
    public string? Warning { get; set; }
    public string? ErrorMessage { get; set; }
}

