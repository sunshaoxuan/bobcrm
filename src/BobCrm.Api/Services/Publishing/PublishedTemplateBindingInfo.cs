using BobCrm.Api.Base;

namespace BobCrm.Api.Services;

/// <summary>
/// 已发布的模板绑定信息
/// </summary>
public record PublishedTemplateBindingInfo(
    string ViewState,
    FormTemplateUsageType UsageType,
    int BindingId,
    int TemplateId,
    string RequiredFunctionCode);

