using System.Net.Http.Json;

namespace BobCrm.App.Services;

public class AccessService
{
    private readonly AuthService _auth;
    public AccessService(AuthService auth) => _auth = auth;

    public async Task<List<AccessRow>> GetAsync(int customerId, CancellationToken ct = default)
    {
        var resp = await _auth.GetWithRefreshAsync($"/api/customers/{customerId}/access");
        if (!resp.IsSuccessStatusCode) return new();
        var list = await resp.Content.ReadFromJsonAsync<List<AccessRow>>(cancellationToken: ct);
        return list ?? new();
    }

    public async Task<bool> UpsertAsync(int customerId, string userId, bool canEdit, CancellationToken ct = default)
    {
        var http = await _auth.CreateClientWithAuthAsync();
        var body = new { userId, canEdit };
        var resp = await http.PostAsJsonAsync($"/api/customers/{customerId}/access", body, ct);
        return resp.IsSuccessStatusCode;
    }

    public record AccessRow(string userId, bool canEdit);
}

