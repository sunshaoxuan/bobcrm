namespace BobCrm.Api.Contracts.DTOs.Access;

/// <summary>
/// 角色功能绑定 DTO
/// </summary>
public record RoleFunctionDto
{
    public Guid RoleId { get; init; }
    public Guid FunctionId { get; init; }
    public int? TemplateBindingId { get; init; }
}
