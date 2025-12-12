namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 更新客户 DTO
/// </summary>
public record UpdateCustomerDto(List<FieldDto> Fields, int? ExpectedVersion, string? Code = null, string? Name = null);
