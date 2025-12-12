namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 访问权限更新 DTO
/// </summary>
public record AccessUpsertDto(string UserId, bool CanEdit);
