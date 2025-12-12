using System.ComponentModel.DataAnnotations;
using BobCrm.Api.Contracts.Requests.User;

namespace BobCrm.Api.Contracts.Requests.User;

/// <summary>
/// 创建用户请求
/// </summary>
public record CreateUserRequest
{
    [Required]
    public string UserName { get; init; } = string.Empty;
    [Required]
    public string Email { get; init; } = string.Empty;
    public string? Password { get; init; }
    public bool EmailConfirmed { get; init; } = true;
    public List<UserRoleAssignmentRequest> Roles { get; init; } = new();
}
