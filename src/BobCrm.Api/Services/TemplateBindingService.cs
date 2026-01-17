using System;
using System.Linq;
using System.Text.Json;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BobCrm.Api.Services;

public class TemplateBindingService
{
    private readonly AppDbContext _db;
    private readonly ILogger<TemplateBindingService> _logger;

    public TemplateBindingService(AppDbContext db, ILogger<TemplateBindingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<TemplateBinding?> GetBindingAsync(string entityType, FormTemplateUsageType usageType, CancellationToken ct = default)
    {
        entityType = entityType.Trim();
        var bindings = await _db.TemplateBindings
            .Include(b => b.Template)
            .Where(b => b.EntityType == entityType && b.UsageType == usageType)
            .OrderBy(b => b.IsSystem) // prefer non-system binding
            .ToListAsync(ct);

        var candidate = bindings.FirstOrDefault(b => IsTemplateUsable(b.Template));
        if (candidate != null)
        {
            return candidate;
        }

        // Fallback: use default TemplateStateBinding when user binding is empty/invalid
        var fallbackViewState = MapUsageToViewState(usageType);
        var fallbackState = await _db.TemplateStateBindings
            .Include(b => b.Template)
            .Where(b => b.EntityType == entityType && b.ViewState == fallbackViewState && b.IsDefault)
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (fallbackState?.Template != null && IsTemplateUsable(fallbackState.Template))
        {
            // Try to reuse any tracked binding that already points to this template
            var reuse = bindings.FirstOrDefault(b => b.TemplateId == fallbackState.TemplateId);
            if (reuse != null)
            {
                return reuse;
            }

            // Synthesized binding (non-tracked) so runtime can still render a usable template
            return new TemplateBinding
            {
                EntityType = entityType,
                UsageType = usageType,
                TemplateId = fallbackState.TemplateId,
                Template = fallbackState.Template,
                IsSystem = true,
                RequiredFunctionCode = fallbackState.Template.RequiredFunctionCode,
                UpdatedBy = "system",
                UpdatedAt = DateTime.UtcNow
            };
        }

        // Last resort: return the first binding even if unusable (preserve previous behavior)
        return bindings.FirstOrDefault();
    }
    public async Task<TemplateBinding> UpsertBindingAsync(
        string entityType,
        FormTemplateUsageType usageType,
        FormTemplate template,
        bool isSystem,
        string updatedBy,
        string? requiredFunctionCode,
        bool saveChanges = true,
        CancellationToken ct = default)
    {
        entityType = entityType.Trim();

        if (!string.IsNullOrWhiteSpace(template.EntityType) &&
            !string.Equals(template.EntityType, entityType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Template entity type mismatch.");
        }

        // 优先从本地缓存查找，由其是在延迟保存的情况下
        var binding = _db.TemplateBindings.Local
            .FirstOrDefault(b => b.EntityType == entityType && b.UsageType == usageType && b.IsSystem == isSystem)
            ?? await _db.TemplateBindings
            .FirstOrDefaultAsync(b => b.EntityType == entityType && b.UsageType == usageType && b.IsSystem == isSystem, ct);

        if (binding == null)
        {
            binding = new TemplateBinding
            {
                EntityType = entityType,
                UsageType = usageType,
                IsSystem = isSystem
            };
            _db.TemplateBindings.Add(binding);
        }

        binding.Template = template;
        binding.RequiredFunctionCode = requiredFunctionCode ?? template.RequiredFunctionCode;
        binding.UpdatedBy = updatedBy;
        binding.UpdatedAt = DateTime.UtcNow;

        if (saveChanges)
        {
            await _db.SaveChangesAsync(ct);
        }

        return binding;
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

    private static string MapUsageToViewState(FormTemplateUsageType usageType) => usageType switch
    {
        FormTemplateUsageType.List => "List",
        FormTemplateUsageType.Edit => "DetailEdit",
        FormTemplateUsageType.Combined => "Create",
        _ => "DetailView"
    };
}
