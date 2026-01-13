namespace BobCrm.Api.Contracts.Requests.Template;

/// <summary>
/// 新增或更新 TemplateStateBinding 请求
/// </summary>
public record UpsertTemplateStateBindingRequest(
    string EntityType,
    string ViewState,
    int TemplateId,
    string? MatchFieldName,
    string? MatchFieldValue,
    int Priority = 0,
    bool IsDefault = false,
    string? RequiredPermission = null);

