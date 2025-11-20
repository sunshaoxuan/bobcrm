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
                // 获取GetInitialDefinition静态方法
                var getDefMethod = entityType.GetMethod(
                    "GetInitialDefinition",
                    BindingFlags.Public | BindingFlags.Static);

                if (getDefMethod == null)
                {
                    _logger.LogWarning("[EntitySync] Type {Type} does not have GetInitialDefinition method, skipping", entityType.FullName);
                    continue;
                }

                var definition = (EntityDefinition?)getDefMethod.Invoke(null, null);
                if (definition == null)
                {
                    _logger.LogWarning("[EntitySync] GetInitialDefinition returned null for {Type}, skipping", entityType.FullName);
                    continue;
                }

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

            await _db.EntityDefinitions.AddAsync(definition);
            await _db.SaveChangesAsync();
            return true;
        }
        else
        {
            // 实体已存在，检查并更新Source字段
            bool needsUpdate = false;

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

            if (needsUpdate)
            {
                existing.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation("[EntitySync] Updated Source fields for: {FullTypeName}",
                    definition.FullTypeName);
                return true;
            }

            _logger.LogDebug("[EntitySync] Entity already exists and is correctly configured: {FullTypeName}",
                definition.FullTypeName);
            return false;
        }
    }

    // New helper to ensure templates and bindings
    private async Task EnsureTemplatesAndBindingsAsync(EntityDefinition entityDef)
    {
        if (_templateService == null || _bindingService == null)
        {
            _logger.LogWarning("Template service or binding service not configured; skipping template generation.");
            return;
        }
        // Ensure default templates (List, Detail, Edit)
        var result = await _templateService.EnsureTemplatesAsync(entityDef, "system");
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

        // 获取初始定义
        var getDefMethod = entityType.GetMethod(
            "GetInitialDefinition",
            BindingFlags.Public | BindingFlags.Static);

        if (getDefMethod == null)
        {
            throw new InvalidOperationException($"Entity type does not have GetInitialDefinition method: {fullTypeName}");
        }

        var initialDef = (EntityDefinition?)getDefMethod.Invoke(null, null);
        if (initialDef == null)
        {
            throw new InvalidOperationException($"GetInitialDefinition returned null for: {fullTypeName}");
        }

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
        existing.Fields = initialDef.Fields;
        existing.Interfaces = initialDef.Interfaces;

        await _db.SaveChangesAsync();

        _logger.LogInformation("[EntitySync] System entity reset completed: {FullTypeName}", fullTypeName);
    }
}
