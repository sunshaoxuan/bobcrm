using System.Net.Http.Json;
using BobCrm.App.Models;

namespace BobCrm.App.Services;

public class MenuService
{
    private readonly AuthService _auth;

    public MenuService(AuthService auth)
    {
        _auth = auth;
    }

    public async Task<List<FunctionMenuNode>> GetManageTreeAsync(CancellationToken ct = default)
    {
        var resp = await _auth.GetWithRefreshAsync("/api/access/functions/manage");
        if (!resp.IsSuccessStatusCode)
        {
            return new List<FunctionMenuNode>();
        }

        var nodes = await resp.Content.ReadFromJsonAsync<List<FunctionMenuNode>>(cancellationToken: ct);
        return nodes ?? new List<FunctionMenuNode>();
    }

    public async Task<FunctionMenuNode?> CreateAsync(CreateMenuNodeRequest request, CancellationToken ct = default)
    {
        var client = await _auth.CreateClientWithAuthAsync();
        var resp = await client.PostAsJsonAsync("/api/access/functions", request, ct);
        if (!resp.IsSuccessStatusCode)
        {
            return null;
        }

        return await resp.Content.ReadFromJsonAsync<FunctionMenuNode>(cancellationToken: ct);
    }

    public async Task<FunctionMenuNode?> UpdateAsync(Guid id, UpdateMenuNodeRequest request, CancellationToken ct = default)
    {
        var client = await _auth.CreateClientWithAuthAsync();
        var resp = await client.PutAsJsonAsync($"/api/access/functions/{id}", request, ct);
        if (!resp.IsSuccessStatusCode)
        {
            return null;
        }

        return await resp.Content.ReadFromJsonAsync<FunctionMenuNode>(cancellationToken: ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var client = await _auth.CreateClientWithAuthAsync();
        var resp = await client.DeleteAsync($"/api/access/functions/{id}", ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> ReorderAsync(List<MenuNodeOrderRequest> requests, CancellationToken ct = default)
    {
        if (requests.Count == 0)
        {
            return true;
        }

        var client = await _auth.CreateClientWithAuthAsync();
        var resp = await client.PostAsJsonAsync("/api/access/functions/reorder", requests, ct);
        return resp.IsSuccessStatusCode;
    }

    public async Task<List<TemplateSummary>> GetTemplatesAsync(CancellationToken ct = default)
    {
        var resp = await _auth.GetWithRefreshAsync("/api/templates");
        if (!resp.IsSuccessStatusCode)
        {
            return new List<TemplateSummary>();
        }

        var templates = await resp.Content.ReadFromJsonAsync<List<TemplateSummary>>(cancellationToken: ct);
        return templates ?? new List<TemplateSummary>();
    }
}
