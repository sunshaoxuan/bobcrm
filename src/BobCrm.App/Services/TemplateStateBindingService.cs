using System.Net.Http.Json;
using BobCrm.Api.Contracts;
using BobCrm.Api.Contracts.DTOs.Template;
using BobCrm.Api.Contracts.Requests.Template;

namespace BobCrm.App.Services;

/// <summary>
/// TemplateStateBinding 管理服务（PLAN-25）
/// </summary>
public class TemplateStateBindingService
{
    private readonly AuthService _auth;

    public TemplateStateBindingService(AuthService auth)
    {
        _auth = auth;
    }

    public async Task<List<TemplateStateBindingDto>> GetAsync(string entityType, string viewState, int? templateId = null, CancellationToken ct = default)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var url = $"/api/templates/state-bindings?entityType={Uri.EscapeDataString(entityType)}&viewState={Uri.EscapeDataString(viewState)}";
        if (templateId.HasValue)
        {
            url += $"&templateId={templateId.Value}";
        }

        var resp = await http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode)
        {
            return new List<TemplateStateBindingDto>();
        }

        return await ApiResponseHelper.ReadDataAsync<List<TemplateStateBindingDto>>(resp) ?? new List<TemplateStateBindingDto>();
    }

    public async Task<TemplateStateBindingDto?> CreateAsync(UpsertTemplateStateBindingRequest request, CancellationToken ct = default)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var resp = await http.PostAsJsonAsync("/api/templates/state-bindings", request, ct);
        if (!resp.IsSuccessStatusCode)
        {
            return null;
        }

        return await ApiResponseHelper.ReadDataAsync<TemplateStateBindingDto>(resp);
    }

    public async Task<TemplateStateBindingDto?> UpdateAsync(int id, UpsertTemplateStateBindingRequest request, CancellationToken ct = default)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var resp = await http.PutAsJsonAsync($"/api/templates/state-bindings/{id}", request, ct);
        if (!resp.IsSuccessStatusCode)
        {
            return null;
        }

        return await ApiResponseHelper.ReadDataAsync<TemplateStateBindingDto>(resp);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var http = await _auth.CreateAuthedClientAsync();
        var resp = await http.DeleteAsync($"/api/templates/state-bindings/{id}", ct);
        return resp.IsSuccessStatusCode;
    }
}

