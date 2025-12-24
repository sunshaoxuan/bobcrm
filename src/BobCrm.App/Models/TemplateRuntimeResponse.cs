namespace BobCrm.App.Models;

public record TemplateRuntimeResponse(
    TemplateBindingDto Binding,
    TemplateDescriptorDto Template,
    bool HasFullAccess,
    IReadOnlyList<string> AppliedScopes);
