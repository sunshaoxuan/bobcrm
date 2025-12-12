using BobCrm.Api.Base;

namespace BobCrm.Api.Contracts.Requests.Template;

/// <summary>
/// 新增或更新模板绑定请求
/// </summary>
public record UpsertTemplateBindingRequest(
    string EntityType,
    FormTemplateUsageType UsageType,
    int TemplateId,
    bool IsSystem,
    string? RequiredFunctionCode);
