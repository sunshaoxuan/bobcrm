using System.Net.Http.Json;
using BobCrm.App.Models;

namespace BobCrm.App.Services;

public class RoleService
{
    private readonly AuthService _auth;

    public RoleService(AuthService auth)
    {
        _auth = auth;
    }

    public async Task<List<RoleProfileDto>> GetRolesAsync(CancellationToken ct = default)
    {
        var resp = await _auth.GetWithRefreshAsync("/api/access/roles");
        if (!resp.IsSuccessStatusCode)
        {
            return new();
        }

        var roles = await resp.Content.ReadFromJsonAsync<List<RoleProfileDto>>(cancellationToken: ct);
        return roles ?? new List<RoleProfileDto>();
    }

    public async Task<RoleProfileDto?> GetRoleAsync(Guid id, CancellationToken ct = default)
    {
        var resp = await _auth.GetWithRefreshAsync($"/api/access/roles/{id}");
        if (!resp.IsSuccessStatusCode)
        {
            return null;
        }

        return await resp.Content.ReadFromJsonAsync<RoleProfileDto>(cancellationToken: ct);
    }

    public async Task<RoleProfileDto?> CreateRoleAsync(CreateRoleRequestDto request, CancellationToken ct = default)
    {
        var client = await _auth.CreateClientWithAuthAsync();
        var resp = await client.PostAsJsonAsync("/api/access/roles", request, ct);
        if (!resp.IsSuccessStatusCode)
        {
            return null;
        }

        return await resp.Content.ReadFromJsonAsync<RoleProfileDto>(cancellationToken: ct);
    }

    public async Task<bool> UpdateRoleAsync(Guid id, UpdateRoleRequestDto request, CancellationToken ct = default)
    {
        var client = await _auth.CreateClientWithAuthAsync();
        var resp = await client.PutAsJsonAsync($"/api/access/roles/{id}", request, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> UpdatePermissionsAsync(Guid id, UpdatePermissionsRequestDto request, CancellationToken ct = default)
    {
        var client = await _auth.CreateClientWithAuthAsync();
        var resp = await client.PutAsJsonAsync($"/api/access/roles/{id}/permissions", request, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<List<FunctionMenuNode>> GetFunctionTreeAsync(CancellationToken ct = default)
    {
        var resp = await _auth.GetWithRefreshAsync("/api/access/functions");
        if (!resp.IsSuccessStatusCode)
        {
            return new();
        }

        var tree = await resp.Content.ReadFromJsonAsync<List<FunctionMenuNode>>(cancellationToken: ct);
        return tree ?? new List<FunctionMenuNode>();
    }
}
