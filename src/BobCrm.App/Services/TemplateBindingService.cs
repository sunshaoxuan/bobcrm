using System.Net.Http.Json;
using System.Text.Json;
using BobCrm.App.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BobCrm.App.Services;

public class TemplateBindingService
{
    private readonly AuthService? _auth;
    private readonly ILogger<TemplateBindingService> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected TemplateBindingService()
    {
        _logger = NullLogger<TemplateBindingService>.Instance;
    }

    public TemplateBindingService(AuthService auth, ILogger<TemplateBindingService> logger)
    {
        _auth = auth;
        _logger = logger ?? NullLogger<TemplateBindingService>.Instance;
    }

    public virtual async Task<bool> HasAssignPermissionAsync(CancellationToken cancellationToken = default)
    {
        if (_auth is null)
        {
            return false;
        }

        try
        {
            var response = await _auth.GetWithRefreshAsync("/api/access/functions/me");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[TemplateBinding] Permission check failed: {Status}", response.StatusCode);
                return false;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var tree = await JsonSerializer.DeserializeAsync<List<FunctionMenuNode>>(stream, _jsonOptions, cancellationToken);
            if (tree is null || tree.Count == 0)
            {
                return false;
            }

            return ContainsFunction(tree, "SYS.TEMPLATE.ASSIGN");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TemplateBinding] Permission check exception");
            return false;
        }
    }

    public virtual async Task<IReadOnlyList<BobCrm.App.Models.FormTemplate>> GetTemplatesAsync(string? entityType, CancellationToken cancellationToken = default)
    {
        if (_auth is null)
        {
            return Array.Empty<BobCrm.App.Models.FormTemplate>();
        }

        try
        {
            var client = await _auth.CreateClientWithAuthAsync();
            var url = string.IsNullOrWhiteSpace(entityType)
                ? "/api/templates"
                : $"/api/templates?entityType={Uri.EscapeDataString(entityType)}";

            var response = await client.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[TemplateBinding] Failed to load templates for {Entity}: {Status}", entityType ?? "*", response.StatusCode);
                return Array.Empty<BobCrm.App.Models.FormTemplate>();
            }

            var templates = await response.Content.ReadFromJsonAsync<List<BobCrm.App.Models.FormTemplate>>(_jsonOptions, cancellationToken);
            return (IReadOnlyList<BobCrm.App.Models.FormTemplate>?)templates ?? Array.Empty<BobCrm.App.Models.FormTemplate>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TemplateBinding] Load templates failed for {Entity}", entityType ?? "*");
            return Array.Empty<BobCrm.App.Models.FormTemplate>();
        }
    }

    public virtual async Task<TemplateBindingDto?> GetBindingAsync(string entityType, BobCrm.App.Models.TemplateUsageType usageType, CancellationToken cancellationToken = default)
    {
        if (_auth is null)
        {
            return null;
        }

        try
        {
            var url = $"/api/templates/bindings/{Uri.EscapeDataString(entityType)}?usageType={(int)usageType}";
            var response = await _auth.GetWithRefreshAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("[TemplateBinding] Binding fetch failed for {Entity}/{Usage}: {Status}", entityType, usageType, response.StatusCode);
                }
                return null;
            }

            var binding = await response.Content.ReadFromJsonAsync<TemplateBindingDto>(_jsonOptions, cancellationToken);
            return binding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TemplateBinding] Binding fetch exception for {Entity}/{Usage}", entityType, usageType);
            return null;
        }
    }

    public virtual async Task<TemplateBindingDto?> UpsertBindingAsync(
        string entityType,
        BobCrm.App.Models.TemplateUsageType usageType,
        int templateId,
        bool isSystem,
        string? requiredFunctionCode,
        CancellationToken cancellationToken = default)
    {
        if (_auth is null)
        {
            return null;
        }

        try
        {
            var client = await _auth.CreateClientWithAuthAsync();
            var payload = new
            {
                EntityType = entityType,
                UsageType = usageType,
                TemplateId = templateId,
                IsSystem = isSystem,
                RequiredFunctionCode = string.IsNullOrWhiteSpace(requiredFunctionCode) ? null : requiredFunctionCode
            };

            var response = await client.PutAsJsonAsync("/api/templates/bindings", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("[TemplateBinding] Save failed for {Entity}/{Usage}: {Status}", entityType, usageType, response.StatusCode);
                return null;
            }

            var binding = await response.Content.ReadFromJsonAsync<TemplateBindingDto>(_jsonOptions, cancellationToken);
            return binding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TemplateBinding] Save exception for {Entity}/{Usage}", entityType, usageType);
            return null;
        }
    }

    private static bool ContainsFunction(IEnumerable<FunctionMenuNode> nodes, string code)
    {
        foreach (var node in nodes)
        {
            if (string.Equals(node.Code, code, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (node.Children is { Count: > 0 } && ContainsFunction(node.Children, code))
            {
                return true;
            }
        }

        return false;
    }
}
