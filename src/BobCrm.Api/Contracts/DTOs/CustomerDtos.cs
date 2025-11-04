namespace BobCrm.Api.Contracts.DTOs;

public record CreateCustomerDto(string Code, string Name);
public record UpdateCustomerDto(List<FieldDto> Fields, int? ExpectedVersion, string? Code = null, string? Name = null);
public record FieldDto(string Key, object Value);
public record AccessUpsertDto(string UserId, bool CanEdit);

