using System;
using System.Net.Http.Json;
using BobCrm.App.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BobCrm.App.Services;

public class TemplateRuntimeClient
{
    private readonly AuthService _auth;
    private readonly ILogger<TemplateRuntimeClient> _logger;

    protected TemplateRuntimeClient()
    {
        _auth = null!;
        _logger = NullLogger<TemplateRuntimeClient>.Instance;
    }

    public TemplateRuntimeClient(AuthService auth, ILogger<TemplateRuntimeClient> logger)
    {
        _auth = auth;
        _logger = logger ?? NullLogger<TemplateRuntimeClient>.Instance;
    }

    public virtual async Task<TemplateRuntimeResponse?> GetRuntimeAsync(
        string entityType,
        TemplateUsageType usageType,
        string? functionOverride = null,
        CancellationToken cancellationToken = default)
    {
        if (_auth is null)
        {
            return null;
        }

        try
        {
            var client = await _auth.CreateClientWithAuthAsync();
            var response = await client.PostAsJsonAsync(
                $"/api/templates/runtime/{entityType}",
                new TemplateRuntimeRequest
                {
                    UsageType = usageType,
                    FunctionCodeOverride = functionOverride
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[TemplateRuntime] Failed to load runtime for {EntityType}/{UsageType}: {Status}",
                    entityType,
                    usageType,
                    response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<TemplateRuntimeResponse>(cancellationToken: cancellationToken);
            return payload;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TemplateRuntime] Runtime fetch failed for {EntityType}/{UsageType}", entityType, usageType);
            return null;
        }
    }
}
