using BobCrm.Api.Base;

namespace BobCrm.Api.Contracts.Requests.Template;

/// <summary>
/// 复制模板请求
/// </summary>
public record CopyTemplateRequest(
    string? Name,
    string? EntityType,
    FormTemplateUsageType? UsageType,
    string? Description);
