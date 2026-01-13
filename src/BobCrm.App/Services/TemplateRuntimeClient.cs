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
        int? templateId = null,
        string? viewState = null,
        Guid? menuNodeId = null,
        int? entityId = null,
        System.Text.Json.JsonElement? entityData = null,
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
                    TemplateId = templateId,
                    ViewState = viewState,
                    MenuNodeId = menuNodeId,
                    FunctionCodeOverride = functionOverride,
                    EntityId = entityId,
                    EntityData = entityData
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

            var payload = await ApiResponseHelper.ReadDataAsync<TemplateRuntimeResponse>(response);
            return payload;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TemplateRuntime] Runtime fetch failed for {EntityType}/{UsageType}", entityType, usageType);
            return null;
        }
    }

    public virtual async Task<(TemplateRuntimeResponse? Runtime, System.Net.HttpStatusCode? Status)> GetRuntimeWithStatusAsync(
        string entityType,
        TemplateUsageType usageType,
        string? functionOverride = null,
        int? templateId = null,
        string? viewState = null,
        Guid? menuNodeId = null,
        int? entityId = null,
        System.Text.Json.JsonElement? entityData = null,
        CancellationToken cancellationToken = default)
    {
        if (_auth is null)
        {
            return (null, null);
        }

        try
        {
            var client = await _auth.CreateClientWithAuthAsync();
            var response = await client.PostAsJsonAsync(
                $"/api/templates/runtime/{entityType}",
                new TemplateRuntimeRequest
                {
                    UsageType = usageType,
                    TemplateId = templateId,
                    ViewState = viewState,
                    MenuNodeId = menuNodeId,
                    FunctionCodeOverride = functionOverride,
                    EntityId = entityId,
                    EntityData = entityData
                },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[TemplateRuntime] Failed to load runtime for {EntityType}/{UsageType}: {Status}",
                    entityType,
                    usageType,
                    response.StatusCode);
                return (null, response.StatusCode);
            }

            var payload = await ApiResponseHelper.ReadDataAsync<TemplateRuntimeResponse>(response);
            return (payload, response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TemplateRuntime] Runtime fetch failed for {EntityType}/{UsageType}", entityType, usageType);
            return (null, null);
        }
    }
}
