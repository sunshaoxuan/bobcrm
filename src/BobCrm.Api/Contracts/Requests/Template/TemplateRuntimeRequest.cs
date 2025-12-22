using System.Text.Json;
using BobCrm.Api.Base;

namespace BobCrm.Api.Contracts.Requests.Template;

/// <summary>
/// 模板运行时请求
/// </summary>
public record TemplateRuntimeRequest(
    FormTemplateUsageType UsageType = FormTemplateUsageType.Detail,
    string? FunctionCodeOverride = null,
    int? EntityId = null,
    JsonElement? EntityData = null);
