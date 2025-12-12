namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 更新字段定义 DTO
/// </summary>
public record UpdateFieldDefDto(
    string DisplayName,
    bool? Required = null,
    object? DefaultValue = null,
    string? Validation = null,
    List<string>? Tags = null,
    List<string>? Actions = null
);
