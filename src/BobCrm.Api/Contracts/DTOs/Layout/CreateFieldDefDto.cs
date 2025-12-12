namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 创建字段定义 DTO
/// </summary>
public record CreateFieldDefDto(
    string Key,
    string DisplayName,
    string DataType,
    bool Required = false,
    object? DefaultValue = null,
    string? Validation = null,
    List<string>? Tags = null,
    List<string>? Actions = null
);
