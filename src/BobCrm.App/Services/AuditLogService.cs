using System.Net.Http.Json;
using System.Web;
using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.Responses.System;

namespace BobCrm.App.Services;

public sealed class AuditLogService
{
    private readonly AuthService _auth;

    public AuditLogService(AuthService auth)
    {
        _auth = auth;
    }

    public async Task<PagedResponse<AuditLogDto>?> SearchAsync(
        int page,
        int pageSize,
        string? module,
        string? operationType,
        string? actor,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken ct = default)
    {
        var http = await _auth.CreateAuthedClientAsync();

        var query = HttpUtility.ParseQueryString(string.Empty);
        query["page"] = page.ToString();
        query["pageSize"] = pageSize.ToString();
        if (!string.IsNullOrWhiteSpace(module)) query["module"] = module;
        if (!string.IsNullOrWhiteSpace(operationType)) query["operationType"] = operationType;
        if (!string.IsNullOrWhiteSpace(actor)) query["actor"] = actor;
        if (fromUtc.HasValue) query["fromUtc"] = fromUtc.Value.ToString("O");
        if (toUtc.HasValue) query["toUtc"] = toUtc.Value.ToString("O");

        var url = "/api/system/audit-logs";
        var queryString = query.ToString();
        if (!string.IsNullOrWhiteSpace(queryString))
        {
            url += "?" + queryString;
        }

        return await http.GetFromJsonAsync<PagedResponse<AuditLogDto>>(url, ct);
    }

    public async Task<List<string>> GetModulesAsync(int limit = 200, CancellationToken ct = default)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var url = $"/api/system/audit-logs/modules?limit={limit}";
        return await http.GetFromJsonAsync<List<string>>(url, ct) ?? new List<string>();
    }
}

