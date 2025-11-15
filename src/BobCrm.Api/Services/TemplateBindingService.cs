using System;
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
        return await _db.TemplateBindings
            .Include(b => b.Template)
            .Where(b => b.EntityType == entityType && b.UsageType == usageType)
            .OrderBy(b => b.IsSystem) // prefer non-system binding
            .FirstOrDefaultAsync(ct);
    }

    public async Task<TemplateBinding> UpsertBindingAsync(
        string entityType,
        FormTemplateUsageType usageType,
        int templateId,
        bool isSystem,
        string updatedBy,
        string? requiredFunctionCode,
        CancellationToken ct = default)
    {
        entityType = entityType.Trim();
        var template = await _db.FormTemplates.FirstOrDefaultAsync(t => t.Id == templateId, ct)
            ?? throw new InvalidOperationException("Template not found.");

        if (!string.IsNullOrWhiteSpace(template.EntityType) &&
            !string.Equals(template.EntityType, entityType, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Template entity type mismatch.");
        }

        var binding = await _db.TemplateBindings
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

        binding.TemplateId = templateId;
        binding.RequiredFunctionCode = requiredFunctionCode ?? template.RequiredFunctionCode;
        binding.UpdatedBy = updatedBy;
        binding.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _db.Entry(binding).Reference(b => b.Template).LoadAsync(ct);
        return binding;
    }
}
