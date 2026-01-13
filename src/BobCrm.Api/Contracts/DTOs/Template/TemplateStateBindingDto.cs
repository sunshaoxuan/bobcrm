namespace BobCrm.Api.Contracts.DTOs.Template;

/// <summary>
/// 模板状态绑定 DTO（用于管理端 CRUD）
/// </summary>
public record TemplateStateBindingDto(
    int Id,
    string EntityType,
    string ViewState,
    int TemplateId,
    string? MatchFieldName,
    string? MatchFieldValue,
    int Priority,
    bool IsDefault,
    string? RequiredPermission,
    DateTime CreatedAt);

