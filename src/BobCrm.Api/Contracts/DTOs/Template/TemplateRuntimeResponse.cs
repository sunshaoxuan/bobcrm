using BobCrm.Api.Contracts.Requests.Template;

namespace BobCrm.Api.Contracts.DTOs.Template;

/// <summary>
/// 模板运行时响应
/// </summary>
public record TemplateRuntimeResponse(
    TemplateBindingDto Binding,
    TemplateDescriptorDto Template,
    bool HasFullAccess,
    IReadOnlyList<string> AppliedScopes);
