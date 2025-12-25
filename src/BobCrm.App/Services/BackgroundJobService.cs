using System.Net.Http.Json;
using System.Web;
using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.Responses.System;

namespace BobCrm.App.Services;

public sealed class BackgroundJobService
{
    private readonly AuthService _auth;

    public BackgroundJobService(AuthService auth)
    {
        _auth = auth;
    }

    public async Task<PagedResponse<BackgroundJobDto>?> GetRecentAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var http = await _auth.CreateAuthedClientAsync();

        var query = HttpUtility.ParseQueryString(string.Empty);
        query["page"] = page.ToString();
        query["pageSize"] = pageSize.ToString();

        var url = "/api/system/jobs";
        var queryString = query.ToString();
        if (!string.IsNullOrWhiteSpace(queryString))
        {
            url += "?" + queryString;
        }

        return await http.GetFromJsonAsync<PagedResponse<BackgroundJobDto>>(url, ct);
    }

    public async Task<BackgroundJobDto?> GetJobAsync(Guid id, CancellationToken ct = default)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var resp = await http.GetFromJsonAsync<SuccessResponse<BackgroundJobDto>>($"/api/system/jobs/{id}", ct);
        return resp?.Data;
    }

    public async Task<List<BackgroundJobLogDto>> GetLogsAsync(Guid id, int limit = 500, CancellationToken ct = default)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var resp = await http.GetFromJsonAsync<SuccessResponse<IReadOnlyList<BackgroundJobLogDto>>>($"/api/system/jobs/{id}/logs?limit={limit}", ct);
        return resp?.Data?.ToList() ?? new List<BackgroundJobLogDto>();
    }

    public async Task CancelAsync(Guid id, CancellationToken ct = default)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var resp = await http.PostAsync($"/api/system/jobs/{id}/cancel", content: null, ct);
        resp.EnsureSuccessStatusCode();
    }
}

