using System;
using System.Collections.Generic;
using BobCrm.Api.Abstractions;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Base.Models.Metadata;
using BobCrm.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace BobCrm.Api.Infrastructure;

/// <summary>
/// 实体定义同步器 - 将系统实体（IBizEntity实现）同步到EntityDefinition表
/// </summary>
public class EntityDefinitionSynchronizer
{
    private readonly AppDbContext _db;
    private readonly ILogger<EntityDefinitionSynchronizer> _logger;
    private readonly IDefaultTemplateService? _templateService;
    private readonly TemplateBindingService? _bindingService;

    public EntityDefinitionSynchronizer(
        AppDbContext db,
        ILogger<EntityDefinitionSynchronizer> logger,
        IDefaultTemplateService? templateService = null,
        TemplateBindingService? bindingService = null)
    {
        _db = db;
        _logger = logger;
        _templateService = templateService;
        _bindingService = bindingService;
    }

    /// <summary>
    /// 同步所有系统实体到EntityDefinition表
    /// </summary>
    public async Task SyncSystemEntitiesAsync()
    {
        _logger.LogInformation("[EntitySync] Starting system entity synchronization...");

        // 查找所有实现IBizEntity的类型
        var entityTypes = typeof(Customer).Assembly.GetTypes()
            .Where(t => typeof(IBizEntity).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .ToList();

        _logger.LogInformation("[EntitySync] Found {Count} IBizEntity implementations", entityTypes.Count);

        foreach (var entityType in entityTypes)
        {
            try
            {
                var definition = BuildInitialDefinition(entityType);
                if (definition == null) continue;

                // 同步实体定义
                await SyncEntityDefinitionAsync(definition);

                // 确保模板和绑定（无论实体是新创建还是已存在）
                await EnsureTemplatesAndBindingsAsync(definition);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EntitySync] Error syncing entity type {Type}", entityType.FullName);
            }
        }

        _logger.LogInformation("[EntitySync] System entity synchronization completed");
    }

    /// <summary>
    /// 同步单个实体定义
    /// </summary>
    private async Task<bool> SyncEntityDefinitionAsync(EntityDefinition definition)
    {
        var existing = await _db.EntityDefinitions
            .Include(ed => ed.Fields)
            .FirstOrDefaultAsync(ed => ed.FullTypeName == definition.FullTypeName);

        if (existing == null)
        {
            _logger.LogInformation("[EntitySync] Creating new entity definition: {FullTypeName}", definition.FullTypeName);
            definition.Source = EntitySource.System;
            definition.CreatedAt = DateTime.UtcNow;
            definition.UpdatedAt = DateTime.UtcNow;

            EnsureFieldMetadata(definition);
            await _db.EntityDefinitions.AddAsync(definition);
            await _db.SaveChangesAsync();
            return true;
        }

        // 实体已存在，检查并更新Source字段
        var needsUpdate = EnsureEntityBasics(existing, definition);

        if (existing.Source != EntitySource.System)
        {
            _logger.LogInformation("[EntitySync] Updating Source to System for: {FullTypeName}",
                definition.FullTypeName);
            existing.Source = EntitySource.System;
            needsUpdate = true;
        }

        // 同步更新所有字段的Source为System（如果不正确的话）
        foreach (var existingField in existing.Fields)
        {
            if (existingField.Source != FieldSource.System)
            {
                existingField.Source = FieldSource.System;
                needsUpdate = true;
            }
        }

        // 补齐缺失的字段元数据
        foreach (var field in definition.Fields)
        {
            var existingField = existing.Fields.FirstOrDefault(f =>
                f.PropertyName.Equals(field.PropertyName, StringComparison.OrdinalIgnoreCase));
            if (existingField == null)
            {
                field.Id = Guid.NewGuid();
                field.EntityDefinitionId = existing.Id;
                existing.Fields.Add(field);
                needsUpdate = true;
                continue;
            }

            if (existingField.DisplayName == null && field.DisplayName != null)
            {
                existingField.DisplayName = field.DisplayName;
                needsUpdate = true;
            }

            if (existingField.SortOrder == 0 && field.SortOrder > 0)
            {
                existingField.SortOrder = field.SortOrder;
                needsUpdate = true;
            }

            if (string.IsNullOrWhiteSpace(existingField.DataType) && !string.IsNullOrWhiteSpace(field.DataType))
            {
                existingField.DataType = field.DataType;
                needsUpdate = true;
            }

            if (!existingField.IsRequiredExplicitlySet && field.IsRequired)
            {
                existingField.IsRequired = true;
                needsUpdate = true;
            }

            if (!existingField.Length.HasValue && field.Length.HasValue)
            {
                existingField.Length = field.Length;
                needsUpdate = true;
            }
        }

        if (needsUpdate)
        {
            existing.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            _logger.LogInformation("[EntitySync] Updated Source fields for: {FullTypeName}",
                definition.FullTypeName);
            return true;
        }

        _logger.LogDebug("[EntitySync] Entity already exists and is correctly configured: {FullTypeName}",
            definition.FullTypeName);
        return false;
    }

    // New helper to ensure templates and bindings
    private async Task EnsureTemplatesAndBindingsAsync(EntityDefinition entityDef)
    {
        if (_templateService == null || _bindingService == null)
        {
            _logger.LogWarning("Template service or binding service not configured; skipping template generation.");
            return;
        }

        if (string.IsNullOrWhiteSpace(entityDef.EntityRoute))
        {
            _logger.LogWarning("[EntitySync] EntityRoute missing for {FullTypeName}, skip template binding", entityDef.FullTypeName);
            return;
        }

        try
        {
            // Ensure default templates (List, Detail, Edit)
            var result = await _templateService.EnsureTemplatesAsync(entityDef, "system");
            if (result.Templates.Count == 0)
            {
                _logger.LogWarning("[EntitySync] No templates generated for {Entity}", entityDef.EntityRoute);
                return;
            }

            // For each usage type, upsert binding
            foreach (var kvp in result.Templates)
            {
                var usage = kvp.Key;
                var template = kvp.Value;
                await _bindingService.UpsertBindingAsync(
                    entityDef.EntityRoute ?? entityDef.EntityName,
                    usage,
                    template.Id,
                    isSystem: true,
                    updatedBy: "system",
                    requiredFunctionCode: null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EntitySync] Failed to generate templates/bindings for {Entity}", entityDef.EntityRoute);
        }
    }

    /// <summary>
    /// 重置系统实体为默认定义（可选功能，供管理员使用）
    /// </summary>
    public async Task ResetSystemEntityAsync(string fullTypeName)
    {
        _logger.LogWarning("[EntitySync] Resetting system entity to default: {FullTypeName}", fullTypeName);

        // 查找对应的类型
        var entityType = typeof(Customer).Assembly.GetTypes()
            .FirstOrDefault(t => t.FullName == fullTypeName);

        if (entityType == null)
        {
            throw new InvalidOperationException($"Entity type not found: {fullTypeName}");
        }

        var initialDef = BuildInitialDefinition(entityType)
            ?? throw new InvalidOperationException($"GetInitialDefinition returned null for: {fullTypeName}");

        // 查找现有定义
        var existing = await _db.EntityDefinitions
            .Include(ed => ed.Fields)
            .Include(ed => ed.Interfaces)
            .FirstOrDefaultAsync(ed => ed.FullTypeName == fullTypeName);

        if (existing == null)
        {
            throw new InvalidOperationException($"Entity definition not found in database: {fullTypeName}");
        }

        if (existing.Source != EntitySource.System)
        {
            throw new InvalidOperationException($"Cannot reset non-system entity: {fullTypeName}");
        }

        // 删除旧的Fields和Interfaces
        _db.FieldMetadatas.RemoveRange(existing.Fields);
        _db.EntityInterfaces.RemoveRange(existing.Interfaces);

        // 更新基本属性
        existing.Namespace = initialDef.Namespace;
        existing.EntityName = initialDef.EntityName;
        existing.EntityRoute = initialDef.EntityRoute;
        existing.DisplayName = initialDef.DisplayName;
        existing.Description = initialDef.Description;
        existing.ApiEndpoint = initialDef.ApiEndpoint;
        existing.StructureType = initialDef.StructureType;
        existing.Status = initialDef.Status;
        existing.IsRootEntity = initialDef.IsRootEntity;
        existing.IsEnabled = initialDef.IsEnabled;
        existing.Order = initialDef.Order;
        existing.Icon = initialDef.Icon;
        existing.Category = initialDef.Category;
        existing.UpdatedAt = DateTime.UtcNow;

        // 添加新的Fields和Interfaces
        EnsureFieldMetadata(initialDef, existing.Id);
        existing.Fields = initialDef.Fields;
        existing.Interfaces = initialDef.Interfaces;

        await _db.SaveChangesAsync();

        _logger.LogInformation("[EntitySync] System entity reset completed: {FullTypeName}", fullTypeName);
    }

    private EntityDefinition? BuildInitialDefinition(Type entityType)
    {
        var getDefMethod = entityType.GetMethod(
            "GetInitialDefinition",
            BindingFlags.Public | BindingFlags.Static);

        if (getDefMethod == null)
        {
            _logger.LogWarning("[EntitySync] Type {Type} does not have GetInitialDefinition method, skipping", entityType.FullName);
            return null;
        }

        var definition = (EntityDefinition?)getDefMethod.Invoke(null, null);
        if (definition == null)
        {
            _logger.LogWarning("[EntitySync] GetInitialDefinition returned null for {Type}, skipping", entityType.FullName);
            return null;
        }

        definition.Namespace ??= entityType.Namespace ?? "BobCrm.Api.Base.Models";
        definition.EntityName ??= entityType.Name;
        definition.FullTypeName ??= entityType.FullName ?? entityType.Name;
        definition.EntityRoute ??= entityType.Name.ToLowerInvariant();
        definition.ApiEndpoint ??= $"/api/{definition.EntityRoute}s";
        definition.DisplayName ??= new Dictionary<string, string?> { ["en"] = entityType.Name };
        EnsureFieldMetadata(definition);
        definition.Interfaces ??= new List<EntityInterface>();
        return definition;
    }

    private static void EnsureFieldMetadata(EntityDefinition definition, Guid? entityIdOverride = null)
    {
        definition.Fields ??= new List<FieldMetadata>();
        var order = 1;
        foreach (var field in definition.Fields)
        {
            field.EntityDefinitionId = entityIdOverride ?? definition.Id;
            field.SortOrder = field.SortOrder == 0 ? order : field.SortOrder;
            field.Source ??= FieldSource.System;
            order++;
        }
    }

    private static bool EnsureEntityBasics(EntityDefinition existing, EntityDefinition definition)
    {
        var updated = false;
        if (string.IsNullOrWhiteSpace(existing.Namespace) && !string.IsNullOrWhiteSpace(definition.Namespace))
        {
            existing.Namespace = definition.Namespace;
            updated = true;
        }
        if (string.IsNullOrWhiteSpace(existing.EntityRoute) && !string.IsNullOrWhiteSpace(definition.EntityRoute))
        {
            existing.EntityRoute = definition.EntityRoute;
            updated = true;
        }
        if (string.IsNullOrWhiteSpace(existing.EntityName) && !string.IsNullOrWhiteSpace(definition.EntityName))
        {
            existing.EntityName = definition.EntityName;
            updated = true;
        }
        if (existing.DisplayName == null && definition.DisplayName != null)
        {
            existing.DisplayName = definition.DisplayName;
            updated = true;
        }
        if (existing.Description == null && definition.Description != null)
        {
            existing.Description = definition.Description;
            updated = true;
        }
        if (string.IsNullOrWhiteSpace(existing.ApiEndpoint) && !string.IsNullOrWhiteSpace(definition.ApiEndpoint))
        {
            existing.ApiEndpoint = definition.ApiEndpoint;
            updated = true;
        }
        return updated;
    }
}
