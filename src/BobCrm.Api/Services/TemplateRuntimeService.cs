using System;
using System.Collections.Generic;
using System.Linq;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Domain;

namespace BobCrm.Api.Services;

public class TemplateRuntimeService
{
    private readonly TemplateBindingService _bindingService;
    private readonly AccessService _accessService;
    private readonly ILogger<TemplateRuntimeService> _logger;

    public TemplateRuntimeService(
        TemplateBindingService bindingService,
        AccessService accessService,
        ILogger<TemplateRuntimeService> logger)
    {
        _bindingService = bindingService;
        _accessService = accessService;
        _logger = logger;
    }

    public async Task<TemplateRuntimeResponse> BuildRuntimeContextAsync(
        string userId,
        string entityType,
        TemplateRuntimeRequest request,
        CancellationToken ct = default)
    {
        request ??= new TemplateRuntimeRequest();
        var usage = request.UsageType;

        var binding = await _bindingService.GetBindingAsync(entityType, usage, ct)
            ?? throw new InvalidOperationException($"Template binding not found for entity '{entityType}' with usage '{usage}'.");

        if (binding.Template == null)
        {
            throw new InvalidOperationException("Binding does not include template data.");
        }

        var requiredFunction = request.FunctionCodeOverride
            ?? binding.RequiredFunctionCode
            ?? binding.Template.RequiredFunctionCode;

        await _accessService.EnsureFunctionAccessAsync(userId, requiredFunction, ct);
        var scopeResult = await _accessService.EvaluateDataScopeAsync(userId, entityType, ct);

        var appliedScopes = DescribeScopes(scopeResult);

        _logger.LogDebug("Runtime context for {EntityType}/{Usage} built with template {TemplateId}", entityType, usage, binding.TemplateId);

        return new TemplateRuntimeResponse(
            binding.ToDto(),
            binding.Template.ToDescriptor(),
            scopeResult.HasFullAccess,
            appliedScopes);
    }

    private static IReadOnlyList<string> DescribeScopes(DataScopeEvaluationResult result)
    {
        if (result.HasFullAccess)
        {
            return new[] { "All" };
        }

        var descriptions = new List<string>();
        foreach (var scope in result.Scopes)
        {
            descriptions.Add($"{scope.Scope.ScopeType}:{scope.Scope.EntityName}");
            if (scope.OrganizationId.HasValue)
            {
                descriptions.Add($"Org:{scope.OrganizationId.Value}");
            }
        }

        return descriptions.Count == 0 ? new[] { "None" } : descriptions;
    }
}
