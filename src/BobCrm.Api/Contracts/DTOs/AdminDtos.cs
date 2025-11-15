namespace BobCrm.Api.Contracts.DTOs;

public record ResetPasswordDto(string NewPassword);

public record UpdateRoleRequest(string? Name, string? Description, bool? IsEnabled);

public record UpdatePermissionsRequest
{
    public List<Guid>? FunctionIds { get; init; }
    public List<DataScopeDto>? DataScopes { get; init; }
    public List<FunctionPermissionSelectionDto>? FunctionPermissions { get; init; }
}

public record FunctionPermissionSelectionDto
{
    public Guid FunctionId { get; init; }
    public int? TemplateBindingId { get; init; }
}

public record DataScopeDto(string EntityName, string ScopeType, string? FilterExpression);

