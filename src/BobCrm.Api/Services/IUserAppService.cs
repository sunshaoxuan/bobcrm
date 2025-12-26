using BobCrm.Api.Contracts.DTOs.User;
using BobCrm.Api.Contracts.Requests.User;
using BobCrm.Api.Contracts.Responses.User;

namespace BobCrm.Api.Services;

public interface IUserAppService
{
    Task<List<UserSummaryDto>> GetUsersAsync(CancellationToken ct = default);
    Task<UserDetailDto> GetUserAsync(string id, string? lang = null, CancellationToken ct = default);
    Task<UserDetailDto> CreateUserAsync(CreateUserRequest request, string? lang = null, CancellationToken ct = default);
    Task<UserDetailDto> UpdateUserAsync(string id, UpdateUserRequest request, string? lang = null, CancellationToken ct = default);
    Task<UserRolesUpdateResponse> UpdateUserRolesAsync(string id, UpdateUserRolesRequest request, string? lang = null, CancellationToken ct = default);
}
