using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using BobCrm.App.Models;

namespace BobCrm.App.Services;

public class RoleService : IRoleService
{
    private readonly AuthService _auth;
    private readonly SemaphoreSlim _functionTreeGate = new(1, 1);
    private List<FunctionMenuNode> _cachedFunctionTree = new();
    private string? _cachedFunctionTreeVersion;

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

    public async Task<FunctionTreeResponse> GetFunctionTreeAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        await _functionTreeGate.WaitAsync(ct);
        try
        {
            var serverVersion = await GetFunctionTreeVersionInternalAsync(ct);

            if (!forceRefresh &&
                _cachedFunctionTree.Count > 0 &&
                !string.IsNullOrWhiteSpace(serverVersion) &&
                string.Equals(serverVersion, _cachedFunctionTreeVersion, StringComparison.Ordinal))
            {
                return new FunctionTreeResponse(CloneTree(_cachedFunctionTree), _cachedFunctionTreeVersion);
            }

            var resp = await _auth.GetWithRefreshAsync("/api/access/functions");
            if (!resp.IsSuccessStatusCode)
            {
                var fallback = _cachedFunctionTree.Count > 0
                    ? CloneTree(_cachedFunctionTree)
                    : new List<FunctionMenuNode>();
                return new FunctionTreeResponse(fallback, serverVersion ?? _cachedFunctionTreeVersion);
            }

            var tree = await resp.Content.ReadFromJsonAsync<List<FunctionMenuNode>>(cancellationToken: ct) ?? new List<FunctionMenuNode>();
            _cachedFunctionTree = tree;
            _cachedFunctionTreeVersion = serverVersion;
            return new FunctionTreeResponse(CloneTree(tree), _cachedFunctionTreeVersion);
        }
        finally
        {
            _functionTreeGate.Release();
        }
    }

    public async Task<string?> GetFunctionTreeVersionAsync(CancellationToken ct = default)
    {
        return await GetFunctionTreeVersionInternalAsync(ct);
    }

    public void InvalidateFunctionTreeCache()
    {
        _cachedFunctionTree = new List<FunctionMenuNode>();
        _cachedFunctionTreeVersion = null;
    }

    private async Task<string?> GetFunctionTreeVersionInternalAsync(CancellationToken ct)
    {
        try
        {
            var resp = await _auth.GetWithRefreshAsync("/api/access/functions/version");
            if (!resp.IsSuccessStatusCode)
            {
                return _cachedFunctionTreeVersion;
            }

            var payload = await resp.Content.ReadFromJsonAsync<FunctionTreeVersionResponse>(cancellationToken: ct);
            return payload?.Version ?? _cachedFunctionTreeVersion;
        }
        catch
        {
            return _cachedFunctionTreeVersion;
        }
    }

    private static List<FunctionMenuNode> CloneTree(List<FunctionMenuNode> nodes)
    {
        return nodes.Select(CloneNode).ToList();
    }

    private static FunctionMenuNode CloneNode(FunctionMenuNode node)
    {
        return new FunctionMenuNode
        {
            Id = node.Id,
            ParentId = node.ParentId,
            Code = node.Code,
            Name = node.Name,
            Route = node.Route,
            Icon = node.Icon,
            IsMenu = node.IsMenu,
            SortOrder = node.SortOrder,
            DisplayNameTranslations = node.DisplayNameTranslations != null
                ? new MultilingualTextDto(node.DisplayNameTranslations)
                : null,
            TemplateOptions = (node.TemplateOptions ?? new List<FunctionTemplateOption>())
                .Select(option => new FunctionTemplateOption
                {
                    BindingId = option.BindingId,
                    TemplateId = option.TemplateId,
                    TemplateName = option.TemplateName,
                    EntityType = option.EntityType,
                    UsageType = option.UsageType,
                    IsSystem = option.IsSystem,
                    IsDefault = option.IsDefault
                }).ToList(),
            TemplateBindings = (node.TemplateBindings ?? new List<FunctionTemplateBindingSummary>())
                .Select(binding => new FunctionTemplateBindingSummary
                {
                    BindingId = binding.BindingId,
                    EntityType = binding.EntityType,
                    UsageType = binding.UsageType,
                    TemplateId = binding.TemplateId,
                    TemplateName = binding.TemplateName,
                    IsSystem = binding.IsSystem,
                    TemplateOptions = (binding.TemplateOptions ?? new List<FunctionTemplateOption>())
                        .Select(option => new FunctionTemplateOption
                        {
                            BindingId = option.BindingId,
                            TemplateId = option.TemplateId,
                            TemplateName = option.TemplateName,
                            EntityType = option.EntityType,
                            UsageType = option.UsageType,
                            IsSystem = option.IsSystem,
                            IsDefault = option.IsDefault
                        }).ToList()
                }).ToList(),
            Children = CloneTree(node.Children)
        };
    }

    private record FunctionTreeVersionResponse(string? Version);
}
