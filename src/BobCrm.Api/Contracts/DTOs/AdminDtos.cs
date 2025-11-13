namespace BobCrm.Api.Contracts.DTOs;

public record ResetPasswordDto(string NewPassword);

public record UpdateRoleRequest(string? Name, string? Description, bool? IsEnabled);

public record UpdatePermissionsRequest(List<Guid>? FunctionIds, List<DataScopeDto>? DataScopes);

public record DataScopeDto(string EntityName, string ScopeType, string? FilterExpression);

