using System.Net.Http.Json;
using BobCrm.App.Models;

namespace BobCrm.App.Services;

public class OrganizationService
{
    private readonly AuthService _authService;

    public OrganizationService(AuthService authService)
    {
        _authService = authService;
    }

    public async Task<List<OrganizationNodeDto>> GetTreeAsync()
    {
        var client = await _authService.CreateAuthedClientAsync();
        var response = await client.GetFromJsonAsync<List<OrganizationNodeDto>>("/api/organizations/tree");
        return response ?? new List<OrganizationNodeDto>();
    }

    public async Task<OrganizationNodeDto> CreateAsync(CreateOrganizationRequest request)
    {
        var client = await _authService.CreateAuthedClientAsync();
        var response = await client.PostAsJsonAsync("/api/organizations", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrganizationNodeDto>())!;
    }

    public async Task<OrganizationNodeDto> UpdateAsync(Guid id, UpdateOrganizationRequest request)
    {
        var client = await _authService.CreateAuthedClientAsync();
        var response = await client.PutAsJsonAsync($"/api/organizations/{id}", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrganizationNodeDto>())!;
    }

    public async Task DeleteAsync(Guid id)
    {
        var client = await _authService.CreateAuthedClientAsync();
        var response = await client.DeleteAsync($"/api/organizations/{id}");
        response.EnsureSuccessStatusCode();
    }
}
