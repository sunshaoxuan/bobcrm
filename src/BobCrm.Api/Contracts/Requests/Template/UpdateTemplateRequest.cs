using BobCrm.Api.Base;

namespace BobCrm.Api.Contracts.Requests.Template;

/// <summary>
/// 更新模板请求
/// </summary>
public record UpdateTemplateRequest(
    string? Name,
    string? EntityType,
    bool? IsUserDefault,
    string? LayoutJson,
    string? Description);
