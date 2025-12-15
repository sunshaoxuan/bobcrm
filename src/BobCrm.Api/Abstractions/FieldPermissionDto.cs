namespace BobCrm.Api.Abstractions;

/// <summary>
/// 字段权限DTO
/// </summary>
public record FieldPermissionDto(
    string FieldName,
    bool CanRead,
    bool CanWrite,
    string? Remarks = null);
