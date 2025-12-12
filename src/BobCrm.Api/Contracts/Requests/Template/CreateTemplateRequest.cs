using BobCrm.Api.Base;

namespace BobCrm.Api.Contracts.Requests.Template;

/// <summary>
/// 创建模板请求
/// </summary>
public record CreateTemplateRequest(
    string Name,
    string? EntityType,
    bool IsUserDefault,
    string? LayoutJson,
    string? Description);
