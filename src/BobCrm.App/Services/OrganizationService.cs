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
        var resp = await _authService.GetWithRefreshAsync("/api/organizations/tree");
        if (!resp.IsSuccessStatusCode)
        {
            return new List<OrganizationNodeDto>();
        }

        var data = await ApiResponseHelper.ReadDataAsync<List<OrganizationNodeDto>>(resp);
        return data ?? new List<OrganizationNodeDto>();
    }

    public async Task<OrganizationNodeDto> CreateAsync(CreateOrganizationRequest request)
    {
        var resp = await _authService.PostAsJsonWithRefreshAsync("/api/organizations", request);
        resp.EnsureSuccessStatusCode();

        var data = await ApiResponseHelper.ReadDataAsync<OrganizationNodeDto>(resp);
        return data ?? throw new InvalidOperationException("Missing organization node payload.");
    }

    public async Task<OrganizationNodeDto> UpdateAsync(Guid id, UpdateOrganizationRequest request)
    {
        var resp = await _authService.PutAsJsonWithRefreshAsync($"/api/organizations/{id}", request);
        resp.EnsureSuccessStatusCode();

        var data = await ApiResponseHelper.ReadDataAsync<OrganizationNodeDto>(resp);
        return data ?? throw new InvalidOperationException("Missing organization node payload.");
    }

    public async Task DeleteAsync(Guid id)
    {
        var resp = await _authService.DeleteWithRefreshAsync($"/api/organizations/{id}");
        resp.EnsureSuccessStatusCode();
    }
}
