using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Contracts.DTOs.Template;
using BobCrm.Api.Contracts.Requests.Template;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;
public class TemplateRuntimeService
{
    private readonly TemplateBindingService _bindingService;
    private readonly AccessService _accessService;
    private readonly ILogger<TemplateRuntimeService> _logger;
    private readonly AppDbContext _db;
    private readonly IDefaultTemplateService _defaultTemplateService;
    private readonly IReflectionPersistenceService _persistenceService;

    public TemplateRuntimeService(
        TemplateBindingService bindingService,
        AccessService accessService,
        AppDbContext db,
        IDefaultTemplateService defaultTemplateService,
        IReflectionPersistenceService persistenceService,
        ILogger<TemplateRuntimeService> logger)
    {
        _bindingService = bindingService;
        _accessService = accessService;
        _db = db;
        _defaultTemplateService = defaultTemplateService;
        _persistenceService = persistenceService;
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
        var usageViewState = MapUsageToViewState(usage);
        var viewState = !string.IsNullOrWhiteSpace(request.ViewState) ? request.ViewState!.Trim() : usageViewState;

        var normalized = entityType?.Trim() ?? string.Empty;
        var altNormalized = normalized.EndsWith("s", StringComparison.OrdinalIgnoreCase)
            ? normalized[..^1]
            : $"{normalized}s";

        var entityData = await ResolveEntityDataAsync(normalized, altNormalized, request, ct);

        // ===== FIX-10: 菜单-模板绑定解耦 =====
        // 使用 MenuNodeId（FunctionNode.Id）作为上下文入口，避免重用 RequiredPermission 字段做路由匹配键。
        if (request.MenuNodeId.HasValue)
        {
            var menuNodeId = request.MenuNodeId.Value;
            var menuNode = await _db.FunctionNodes
                .AsNoTracking()
                .Include(n => n.TemplateStateBinding)
                .ThenInclude(b => b!.Template)
                .FirstOrDefaultAsync(n => n.Id == menuNodeId, ct);

            if (menuNode == null)
            {
                throw new KeyNotFoundException("Menu node not found.");
            }

            await _accessService.EnsureFunctionAccessAsync(userId, menuNode.Code, ct);

            if (!menuNode.TemplateStateBindingId.HasValue)
            {
                throw new UnauthorizedAccessException("Forbidden.");
            }

            var menuBinding = menuNode.TemplateStateBinding;
            if (menuBinding == null || menuBinding.Id != menuNode.TemplateStateBindingId.Value)
            {
                menuBinding = await _db.TemplateStateBindings
                    .AsNoTracking()
                    .Include(b => b.Template)
                    .FirstOrDefaultAsync(b => b.Id == menuNode.TemplateStateBindingId.Value, ct);
            }

            if (menuBinding == null)
            {
                throw new UnauthorizedAccessException("Forbidden.");
            }

            if (!(menuBinding.EntityType == normalized || menuBinding.EntityType == altNormalized) ||
                !string.Equals(menuBinding.ViewState, viewState, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Forbidden.");
            }

            if (request.TemplateId.HasValue && request.TemplateId.Value != menuBinding.TemplateId)
            {
                // 防止通过篡改 tid 绕过菜单绑定（必须与菜单绑定一致）
                throw new UnauthorizedAccessException("Forbidden.");
            }

            var template = menuBinding.Template;
            if (!IsTemplateUsable(template))
            {
                template = await _db.FormTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == menuBinding.TemplateId, ct);
            }
            if (!IsTemplateUsable(template))
            {
                throw new InvalidOperationException("Template unusable.");
            }

            var scopeMenu = await _accessService.EvaluateDataScopeAsync(userId, entityType ?? "", ct);
            var appliedScopesMenu = DescribeScopes(scopeMenu);

            var bindingDto = new TemplateBindingDto(
                menuBinding.Id,
                normalized,
                usage,
                menuBinding.TemplateId,
                true,
                menuNode.Code,
                "runtime",
                DateTime.UtcNow);

            return new TemplateRuntimeResponse(
                bindingDto,
                template!.ToDescriptor(),
                scopeMenu.HasFullAccess,
                appliedScopesMenu);
        }

        // 强指定模板（tid）：必须证明该用户可通过某个可访问的菜单节点抵达该模板（否则视为越权访问）
        if (request.TemplateId.HasValue)
        {
            var forcedTemplateId = request.TemplateId.Value;
            var forcedTemplate = await _db.FormTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == forcedTemplateId, ct);
            if (forcedTemplate == null)
            {
                throw new KeyNotFoundException("Template not found.");
            }

            var candidateMenuNodes = await _db.FunctionNodes
                .AsNoTracking()
                .Where(n => n.TemplateStateBindingId.HasValue)
                .Join(
                    _db.TemplateStateBindings.AsNoTracking(),
                    n => n.TemplateStateBindingId,
                    b => b.Id,
                    (n, b) => new { Node = n, Binding = b })
                .Where(x =>
                    (x.Binding.EntityType == normalized || x.Binding.EntityType == altNormalized) &&
                    string.Equals(x.Binding.ViewState, viewState, StringComparison.OrdinalIgnoreCase) &&
                    x.Binding.TemplateId == forcedTemplateId)
                .ToListAsync(ct);

            (BobCrm.Api.Base.Models.FunctionNode Node, BobCrm.Api.Base.Models.TemplateStateBinding Binding)? allowedByNode = null;
            foreach (var x in candidateMenuNodes)
            {
                if (await _accessService.HasFunctionAccessAsync(userId, x.Node.Code, ct))
                {
                    allowedByNode = (x.Node, x.Binding);
                    break;
                }
            }

            if (allowedByNode == null)
            {
                throw new UnauthorizedAccessException("Forbidden.");
            }

            await _accessService.EnsureFunctionAccessAsync(userId, allowedByNode.Value.Node.Code, ct);
            var scopeForced = await _accessService.EvaluateDataScopeAsync(userId, entityType ?? "", ct);
            var appliedScopesForced = DescribeScopes(scopeForced);

            var bindingDto = new TemplateBindingDto(
                allowedByNode.Value.Binding.Id,
                normalized,
                usage,
                forcedTemplateId,
                true,
                allowedByNode.Value.Node.Code,
                "runtime",
                DateTime.UtcNow);

            return new TemplateRuntimeResponse(
                bindingDto,
                forcedTemplate.ToDescriptor(),
                scopeForced.HasFullAccess,
                appliedScopesForced);
        }

        var binding = await _bindingService.GetBindingAsync(normalized, usage, ct)
                      ?? await _bindingService.GetBindingAsync(altNormalized, usage, ct);

        if (binding == null)
        {
             _logger.LogWarning("[TemplateRuntime] No binding found for {Normalized}/{Usage}", normalized, usage);
        }
        else
        {
             _logger.LogWarning("[TemplateRuntime] Found binding: Id={Id}, IsSystem={IsSystem}, TemplateId={TemplateId}", binding.Id, binding.IsSystem, binding.TemplateId);
        }

        if (binding == null || !IsTemplateUsable(binding.Template))
        {
            if (binding != null)
            {
                 _logger.LogWarning("[TemplateRuntime] Binding found but template unusable. TemplateId={TemplateId}, LayoutJsonLength={Length}", 
                     binding.TemplateId, binding.Template?.LayoutJson?.Length ?? 0);
                 
                 // Log reasons
                 if (binding.Template == null) _logger.LogWarning("[TemplateRuntime] Template is null");
                 else if (string.IsNullOrWhiteSpace(binding.Template.LayoutJson)) _logger.LogWarning("[TemplateRuntime] LayoutJson is empty");
                 else _logger.LogWarning("[TemplateRuntime] LayoutJson validation failed. Content start: {Content}", 
                     binding.Template.LayoutJson.Length > 100 ? binding.Template.LayoutJson[..100] : binding.Template.LayoutJson);
            }

            binding = await RegenerateAndReloadBindingAsync(normalized, altNormalized, usage, ct);
        }

        if (binding == null || binding.Template == null)
        {
            throw new InvalidOperationException($"Template binding not found for entity '{entityType}' with usage '{usage}'.");
        }

        if (binding.Template == null)
        {
            throw new InvalidOperationException("Binding does not include template data.");
        }

        var bindingToUse = await ApplyPolymorphicViewBindingAsync(binding, normalized, altNormalized, usageViewState, usage, entityData, ct);

        var requiredFunction = request.FunctionCodeOverride
            ?? bindingToUse.RequiredFunctionCode
            ?? bindingToUse.Template!.RequiredFunctionCode;

        await _accessService.EnsureFunctionAccessAsync(userId, requiredFunction, ct);
        var scopeResult = await _accessService.EvaluateDataScopeAsync(userId, entityType ?? "", ct);
        
        var appliedScopes = DescribeScopes(scopeResult);

        _logger.LogDebug("Runtime context for {EntityType}/{Usage} built with template {TemplateId}", entityType, usage, binding.TemplateId);

        _logger.LogWarning("Runtime context for {EntityType}/{Usage} built with template {TemplateId}. Final LayoutJson Length: {Length}", 
            entityType, usage, bindingToUse.TemplateId, bindingToUse.Template?.LayoutJson?.Length ?? 0);

        return new TemplateRuntimeResponse(
            ToBindingDto(bindingToUse),
            bindingToUse.Template!.ToDescriptor(),
            scopeResult.HasFullAccess,
            appliedScopes);
    }

    private static TemplateBindingDto ToBindingDto(TemplateBinding binding) =>
        new(
            binding.Id,
            binding.EntityType,
            binding.UsageType,
            binding.TemplateId,
            binding.IsSystem,
            binding.RequiredFunctionCode,
            binding.UpdatedBy,
            binding.UpdatedAt);

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

    private static bool IsTemplateUsable(FormTemplate? template)
    {
        if (template == null) return false;
        if (string.IsNullOrWhiteSpace(template.LayoutJson)) return false;

        try
        {
            using var doc = JsonDocument.Parse(template.LayoutJson);
            return doc.RootElement.ValueKind switch
            {
                JsonValueKind.Array => doc.RootElement.GetArrayLength() > 0,
                JsonValueKind.Object => doc.RootElement.EnumerateObject().Any()
                                        || (doc.RootElement.TryGetProperty("items", out var items)
                                            && items.ValueKind == JsonValueKind.Object
                                            && items.EnumerateObject().Any()),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    private static string MapUsageToViewState(FormTemplateUsageType usage)
        => usage switch
        {
            FormTemplateUsageType.List => "List",
            FormTemplateUsageType.Edit => "DetailEdit",
            FormTemplateUsageType.Combined => "Create",
            _ => "DetailView"
        };

    private async Task<JsonElement?> ResolveEntityDataAsync(
        string normalized,
        string altNormalized,
        TemplateRuntimeRequest request,
        CancellationToken ct)
    {
        if (request.EntityData.HasValue)
        {
            return request.EntityData.Value;
        }

        if (!request.EntityId.HasValue)
        {
            return null;
        }

        try
        {
            var entity = await _db.EntityDefinitions
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                        (e.EntityRoute ?? string.Empty).Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
                        (e.EntityName ?? string.Empty).Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
                        (e.EntityRoute ?? string.Empty).Equals(altNormalized, StringComparison.OrdinalIgnoreCase) ||
                        (e.EntityName ?? string.Empty).Equals(altNormalized, StringComparison.OrdinalIgnoreCase),
                    ct);

            if (string.IsNullOrWhiteSpace(entity?.FullTypeName))
            {
                return null;
            }

            var obj = await _persistenceService.GetByIdAsync(entity.FullTypeName, request.EntityId.Value);
            if (obj == null)
            {
                return null;
            }

            return JsonSerializer.SerializeToElement(obj, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[TemplateRuntime] ResolveEntityDataAsync failed for {EntityType}#{EntityId}", normalized, request.EntityId);
            return null;
        }
    }

    private async Task<TemplateBinding> ApplyPolymorphicViewBindingAsync(
        TemplateBinding binding,
        string normalized,
        string altNormalized,
        string viewState,
        FormTemplateUsageType usage,
        JsonElement? entityData,
        CancellationToken ct)
    {
        if (!entityData.HasValue)
        {
            return binding;
        }

        var stateBindings = await _db.TemplateStateBindings
            .AsNoTracking()
            .Where(b =>
                (b.EntityType == normalized || b.EntityType == altNormalized) &&
                b.ViewState == viewState)
            .ToListAsync(ct);

        if (stateBindings.Count == 0)
        {
            return binding;
        }

        var selectedTemplateId = TemplateStateBindingRuleEngine.SelectTemplateId(stateBindings, entityData);
        if (!selectedTemplateId.HasValue || selectedTemplateId.Value == binding.TemplateId)
        {
            return binding;
        }

        var template = await _db.FormTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == selectedTemplateId.Value, ct);

        if (!IsTemplateUsable(template))
        {
            return binding;
        }

        _logger.LogDebug(
            "[TemplateRuntime] Polymorphic binding applied for {EntityType}/{ViewState}: {OldTemplate} -> {NewTemplate}",
            normalized,
            viewState,
            binding.TemplateId,
            selectedTemplateId.Value);

        return new TemplateBinding
        {
            EntityType = binding.EntityType,
            UsageType = usage,
            TemplateId = selectedTemplateId.Value,
            Template = template!,
            IsSystem = true,
            RequiredFunctionCode = binding.RequiredFunctionCode ?? template!.RequiredFunctionCode,
            UpdatedBy = "runtime",
            UpdatedAt = DateTime.UtcNow
        };
    }

    private async Task<TemplateBinding?> RegenerateAndReloadBindingAsync(
        string normalized,
        string altNormalized,
        FormTemplateUsageType usage,
        CancellationToken ct)
    {
        try
        {
            var entity = await _db.EntityDefinitions
                .Include(e => e.Fields)
                .FirstOrDefaultAsync(e =>
                    (e.EntityRoute ?? string.Empty).Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
                    (e.EntityName ?? string.Empty).Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
                    (e.EntityRoute ?? string.Empty).Equals(altNormalized, StringComparison.OrdinalIgnoreCase) ||
                    (e.EntityName ?? string.Empty).Equals(altNormalized, StringComparison.OrdinalIgnoreCase),
                    ct);

            if (entity == null)
            {
                _logger.LogWarning("[TemplateRuntime] Cannot regenerate template: entity {EntityType} not found", normalized);
                return await _bindingService.GetBindingAsync(normalized, usage, ct)
                       ?? await _bindingService.GetBindingAsync(altNormalized, usage, ct);
            }

            var result = await _defaultTemplateService.EnsureTemplatesAsync(entity, "runtime", force: true, ct: ct);
            _logger.LogInformation(
                "[TemplateRuntime] Regenerated templates for {EntityType}, created={Created}, updated={Updated}",
                normalized, result.Created.Count, result.Updated.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[TemplateRuntime] Failed to regenerate template for {EntityType}", normalized);
        }

        return await _bindingService.GetBindingAsync(normalized, usage, ct)
               ?? await _bindingService.GetBindingAsync(altNormalized, usage, ct);
    }
}
