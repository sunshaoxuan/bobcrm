using System.Net.Http.Json;
using BobCrm.App.Models;

namespace BobCrm.App.Services;

public class UserService
{
    private readonly AuthService _auth;

    public UserService(AuthService auth)
    {
        _auth = auth;
    }

    public async Task<List<UserSummaryDto>> GetUsersAsync(CancellationToken ct = default)
    {
        var resp = await _auth.GetWithRefreshAsync("/api/users");
        if (!resp.IsSuccessStatusCode)
        {
            return new();
        }

        var data = await resp.Content.ReadFromJsonAsync<List<UserSummaryDto>>(cancellationToken: ct);
        return data ?? new List<UserSummaryDto>();
    }

    public async Task<UserDetailDto?> GetUserAsync(string id, CancellationToken ct = default)
    {
        var resp = await _auth.GetWithRefreshAsync($"/api/users/{id}");
        if (!resp.IsSuccessStatusCode)
        {
            return null;
        }

        return await resp.Content.ReadFromJsonAsync<UserDetailDto>(cancellationToken: ct);
    }

    public async Task<UserDetailDto?> CreateUserAsync(CreateUserRequestDto request, CancellationToken ct = default)
    {
        var client = await _auth.CreateClientWithAuthAsync();
        var resp = await client.PostAsJsonAsync("/api/users", request, ct);
        if (!resp.IsSuccessStatusCode)
        {
            return null;
        }

        return await resp.Content.ReadFromJsonAsync<UserDetailDto>(cancellationToken: ct);
    }

    public async Task<bool> UpdateUserAsync(string id, UpdateUserRequestDto request, CancellationToken ct = default)
    {
        var client = await _auth.CreateClientWithAuthAsync();
        var resp = await client.PutAsJsonAsync($"/api/users/{id}", request, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateRolesAsync(string id, IEnumerable<Guid> roleIds, CancellationToken ct = default)
    {
        var client = await _auth.CreateClientWithAuthAsync();
        var body = new UpdateUserRolesRequestDto
        {
            Roles = roleIds.Select(r => new UserRoleAssignmentRequestDto { RoleId = r }).ToList()
        };
        var resp = await client.PutAsJsonAsync($"/api/users/{id}/roles", body, ct);
        return resp.IsSuccessStatusCode;
    }
}
