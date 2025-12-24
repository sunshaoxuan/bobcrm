using System;
using System.Collections.Generic;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BobCrm.Api.Services;

/// <summary>
/// 实体发布服务
/// 负责实体的完整发布流程：验证 -> 生成DDL -> 执行DDL -> 更新状态
/// </summary>
public class EntityPublishingService : IEntityPublishingService
{
    private readonly AppDbContext _db;
    private readonly PostgreSQLDDLGenerator _ddlGenerator;
    private readonly DDLExecutionService _ddlExecutor;
    private readonly IEntityLockService _lockService;
    private readonly TemplateBindingService _templateBindingService;
    private readonly AccessService _accessService;
    private readonly ILogger<EntityPublishingService> _logger;
    private readonly IDefaultTemplateService _defaultTemplateService;
    private readonly IConfiguration _configuration;

    public EntityPublishingService(
        AppDbContext db,
        PostgreSQLDDLGenerator ddlGenerator,
        DDLExecutionService ddlExecutor,
        IEntityLockService lockService,
        TemplateBindingService templateBindingService,
        AccessService accessService,
        IDefaultTemplateService defaultTemplateService,
        IConfiguration configuration,
        ILogger<EntityPublishingService> logger)
    {
        _db = db;
        _ddlGenerator = ddlGenerator;
        _ddlExecutor = ddlExecutor;
        _lockService = lockService;
        _templateBindingService = templateBindingService;
        _accessService = accessService;
        _defaultTemplateService = defaultTemplateService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 发布新实体（CREATE TABLE）
    /// </summary>
    public async Task<PublishResult> PublishNewEntityAsync(Guid entityDefinitionId, string? publishedBy = null)
        => await PublishNewEntityInternalAsync(
            entityDefinitionId,
            publishedBy,
            PublishContext.CreateRoot());

    private async Task<PublishResult> PublishNewEntityInternalAsync(
        Guid entityDefinitionId,
        string? publishedBy,
        PublishContext context)
    {
        var result = new PublishResult { EntityDefinitionId = entityDefinitionId };

        try
        {
            // 1. 加载实体定义
            var entity = await _db.EntityDefinitions
                .Include(e => e.Fields)
                .Include(e => e.Interfaces)
                .FirstOrDefaultAsync(e => e.Id == entityDefinitionId);

            if (entity == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Entity definition {entityDefinitionId} not found";
                return result;
            }

            _logger.LogInformation("[Publish] Publishing new entity: {EntityName}", entity.EntityName);

            // 2. 验证实体状态
            if (entity.Status != EntityStatus.Draft)
            {
                result.Success = false;
                result.ErrorMessage = $"Entity status is {entity.Status}, expected Draft";
                return result;
            }

            if (!context.Visited.Add(entity.Id))
            {
                result.Success = false;
                result.ErrorMessage = $"Cyclic publish dependency detected for entity {entity.EntityName}";
                return result;
            }

            // 2.5 AggVO 级联发布：确保 Lookup 依赖实体已发布
            var cascade = await EnsureLookupDependenciesPublishedAsync(entity, publishedBy, context);
            if (!cascade.Success)
            {
                result.Success = false;
                result.ErrorMessage = cascade.ErrorMessage;
                return result;
            }

            // 3. 检查表是否已存在
            var tableName = entity.DefaultTableName;
            var tableExists = await _ddlExecutor.TableExistsAsync(tableName);
            if (tableExists)
            {
                result.Success = false;
                result.ErrorMessage = $"Table '{tableName}' already exists";
                return result;
            }

            // 4. 应用接口字段（Base, Archive, Audit等）
            await ApplyInterfaceFieldsAsync(entity);

            // 5. 验证枚举引用
            var enumValidation = await ValidateEnumReferencesAsync(entity);
            if (!enumValidation.IsValid)
            {
                result.Success = false;
                result.ErrorMessage = enumValidation.ErrorMessage;
                return result;
            }

            // 6. ?? Lookup 引用?体（防止物理外??建失?）
            var lookupValidation = await ValidateLookupReferencesAsync(entity);
            if (!lookupValidation.IsValid)
            {
                result.Success = false;
                result.ErrorMessage = lookupValidation.ErrorMessage;
                return result;
            }

            // 6. 生成CREATE TABLE DDL
            var createScript = _ddlGenerator.GenerateCreateTableScript(entity);
            result.DDLScript = createScript;

            _logger.LogInformation("[Publish] Generated CREATE TABLE script:\n{Script}", createScript);

            // 6. 执行DDL
            var scriptRecord = await _ddlExecutor.ExecuteDDLAsync(
                entityDefinitionId,
                DDLScriptType.Create,
                createScript,
                publishedBy
            );

            result.ScriptId = scriptRecord.Id;

            if (scriptRecord.Status != DDLScriptStatus.Success)
            {
                result.Success = false;
                result.ErrorMessage = scriptRecord.ErrorMessage;
                return result;
            }

            await using var transaction = _db.Database.IsRelational()
                ? await _db.Database.BeginTransactionAsync()
                : null;

            // 7. 更新实体状态为Published
            entity.Status = EntityStatus.Published;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = publishedBy;
            if (context.IsCascadeChild)
            {
                entity.Source = EntitySource.System;
            }
            await _db.SaveChangesAsync();

            // 8. 锁定实体定义（防止发布后误修改关键属性）
            await _lockService.LockEntityAsync(entityDefinitionId, "Entity published");

            if (!context.IsCascadeChild)
            {
                await GenerateTemplatesAndMenusAsync(entity, publishedBy, result);
            }

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }
            result.Success = true;
            _logger.LogInformation("[Publish] ✓ Entity {EntityName} published successfully, locked, and default template ensured", entity.EntityName);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "[Publish] ✗ Failed to publish entity: {Error}", ex.Message);
        }

        return result;
    }

    /// <summary>
    /// 发布实体修改（ALTER TABLE）
    /// </summary>
    public async Task<PublishResult> PublishEntityChangesAsync(Guid entityDefinitionId, string? publishedBy = null)
        => await PublishEntityChangesInternalAsync(
            entityDefinitionId,
            publishedBy,
            PublishContext.CreateRoot());

    public async Task<WithdrawResult> WithdrawAsync(Guid entityDefinitionId, string? withdrawnBy = null)
    {
        var result = new WithdrawResult { EntityDefinitionId = entityDefinitionId };

        try
        {
            var entity = await _db.EntityDefinitions
                .Include(e => e.Fields)
                .Include(e => e.Interfaces)
                .FirstOrDefaultAsync(e => e.Id == entityDefinitionId);

            if (entity == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Entity definition {entityDefinitionId} not found";
                return result;
            }

            if (entity.Status == EntityStatus.Draft)
            {
                result.Success = false;
                result.ErrorMessage = "Draft entities cannot be withdrawn. Delete it instead.";
                return result;
            }

            var referenced = await IsReferencedByPublishedEntitiesAsync(entity);
            if (referenced)
            {
                result.Success = false;
                result.ErrorMessage = $"Entity '{entity.EntityName}' is referenced by other published entities. Withdrawal is not allowed.";
                return result;
            }

            var mode = ResolveWithdrawalMode();
            result.Mode = mode;

            if (string.Equals(mode, WithdrawalModes.Physical, StringComparison.OrdinalIgnoreCase))
            {
                var dropSql = _ddlGenerator.GenerateDropTableScript(entity);
                result.DDLScript = dropSql;

                var scriptRecord = await _ddlExecutor.ExecuteDDLAsync(
                    entityDefinitionId,
                    DDLScriptType.Drop,
                    dropSql,
                    withdrawnBy);

                result.ScriptId = scriptRecord.Id;
                if (scriptRecord.Status != DDLScriptStatus.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = scriptRecord.ErrorMessage;
                    return result;
                }
            }

            await using var transaction = _db.Database.IsRelational()
                ? await _db.Database.BeginTransactionAsync()
                : null;
            entity.Status = EntityStatus.Withdrawn;
            entity.IsEnabled = false;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = withdrawnBy;
            await _db.SaveChangesAsync();

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "[Withdraw] ✗ Failed to withdraw entity: {Error}", ex.Message);
        }

        return result;
    }

    private async Task<PublishResult> PublishEntityChangesInternalAsync(
        Guid entityDefinitionId,
        string? publishedBy,
        PublishContext context)
    {
        var result = new PublishResult { EntityDefinitionId = entityDefinitionId };

        try
        {
            // 1. 加载实体定义
            var entity = await _db.EntityDefinitions
                .Include(e => e.Fields)
                .Include(e => e.Interfaces)
                .FirstOrDefaultAsync(e => e.Id == entityDefinitionId);

            if (entity == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Entity definition {entityDefinitionId} not found";
                return result;
            }

            _logger.LogInformation("[Publish] Publishing changes for entity: {EntityName}", entity.EntityName);

            // 2. 验证实体状态
            if (entity.Status != EntityStatus.Modified)
        {
            result.Success = false;
            result.ErrorMessage = $"Entity status is {entity.Status}, expected Modified";
            return result;
        }

        if (!context.Visited.Add(entity.Id))
        {
            result.Success = false;
            result.ErrorMessage = $"Cyclic publish dependency detected for entity {entity.EntityName}";
            return result;
        }

        // 2.5 AggVO 级联发布：确保 Lookup 依赖实体已发布
        var cascade = await EnsureLookupDependenciesPublishedAsync(entity, publishedBy, context);
        if (!cascade.Success)
        {
            result.Success = false;
            result.ErrorMessage = cascade.ErrorMessage;
            return result;
        }

        // 3. 验证枚举引用
        var enumValidation = await ValidateEnumReferencesAsync(entity);
        if (!enumValidation.IsValid)
        {
            result.Success = false;
            result.ErrorMessage = enumValidation.ErrorMessage;
            return result;
        }

        // 4. 检查表是否存在
        var lookupValidation = await ValidateLookupReferencesAsync(entity);
        if (!lookupValidation.IsValid)
        {
            result.Success = false;
            result.ErrorMessage = lookupValidation.ErrorMessage;
            return result;
        }

        var tableName = entity.DefaultTableName;
        var tableExists = await _ddlExecutor.TableExistsAsync(tableName);
        if (!tableExists)
        {
            result.Success = false;
            result.ErrorMessage = $"Table '{tableName}' does not exist. Use PublishNewEntity instead.";
            return result;
        }

            // 5. 分析变更
            var changeAnalysis = await AnalyzeChangesAsync(entity);
            result.ChangeAnalysis = changeAnalysis;

            // 5. 验证变更（如果实体被锁定，只允许添加字段和增加长度）
            if (entity.IsLocked)
            {
                if (changeAnalysis.HasDestructiveChanges)
                {
                    result.Success = false;
                    result.ErrorMessage = "Entity is locked. Only adding fields or increasing field lengths is allowed.";
                    return result;
                }
            }

            // 6. 生成ALTER TABLE DDL
            var alterScript = GenerateAlterScript(entity, changeAnalysis);
            result.DDLScript = alterScript;

            if (string.IsNullOrEmpty(alterScript))
            {
                result.Success = false;
                result.ErrorMessage = "No changes detected";
                return result;
            }

            _logger.LogInformation("[Publish] Generated ALTER TABLE script:\n{Script}", alterScript);

            // 7. 执行DDL
            var scriptRecord = await _ddlExecutor.ExecuteDDLAsync(
                entityDefinitionId,
                DDLScriptType.Alter,
                alterScript,
                publishedBy
            );

            result.ScriptId = scriptRecord.Id;

            if (scriptRecord.Status != DDLScriptStatus.Success)
            {
                result.Success = false;
                result.ErrorMessage = scriptRecord.ErrorMessage;
                return result;
            }

            await using var transaction = _db.Database.IsRelational()
                ? await _db.Database.BeginTransactionAsync()
                : null;

            // 8. 更新实体状态为Published
            entity.Status = EntityStatus.Published;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = publishedBy;
            if (context.IsCascadeChild)
            {
                entity.Source = EntitySource.System;
            }
            await _db.SaveChangesAsync();

            if (!context.IsCascadeChild)
            {
                await GenerateTemplatesAndMenusAsync(entity, publishedBy, result);
            }

            if (transaction != null)
            {
                await transaction.CommitAsync();
            }
            result.Success = true;
            _logger.LogInformation("[Publish] ✓ Entity {EntityName} changes published successfully and default template updated", entity.EntityName);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "[Publish] ✗ Failed to publish entity changes: {Error}", ex.Message);
        }

        return result;
    }

    private async Task<(bool Success, string? ErrorMessage)> EnsureLookupDependenciesPublishedAsync(
        EntityDefinition entity,
        string? publishedBy,
        PublishContext context)
    {
        var dependencyNames = entity.Fields
            .Where(f => !f.IsDeleted && !string.IsNullOrWhiteSpace(f.LookupEntityName))
            .Select(f => f.LookupEntityName!.Trim())
            .Where(name => !string.Equals(name, entity.EntityName, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (dependencyNames.Count == 0)
        {
            return (true, null);
        }

        var dependencyLower = dependencyNames
            .Select(n => n.ToLowerInvariant())
            .ToList();

        var candidates = await _db.EntityDefinitions
            .Include(e => e.Fields)
            .Include(e => e.Interfaces)
            .Where(e => dependencyLower.Contains(e.EntityName.ToLower()))
            .ToListAsync();

        foreach (var dependencyName in dependencyNames)
        {
            var dependency = candidates.FirstOrDefault(e =>
                string.Equals(e.EntityName, dependencyName, StringComparison.OrdinalIgnoreCase));

            if (dependency == null)
            {
                return (false, $"Lookup referenced entities not found or not published: {dependencyName}");
            }

            if (dependency.Status is not (EntityStatus.Draft or EntityStatus.Withdrawn))
            {
                continue;
            }

            var childContext = context.AsCascadeChild();
            PublishResult publishResult;

            if (dependency.Status == EntityStatus.Withdrawn)
            {
                publishResult = await RepublishWithdrawnEntityAsync(dependency, publishedBy, childContext);
            }
            else
            {
                publishResult = await PublishNewEntityInternalAsync(dependency.Id, publishedBy, childContext);
            }

            if (!publishResult.Success)
            {
                return (false, $"Cascade publish failed for entity {dependency.EntityName}: {publishResult.ErrorMessage}");
            }
        }

        return (true, null);
    }

    private async Task<PublishResult> RepublishWithdrawnEntityAsync(
        EntityDefinition entity,
        string? publishedBy,
        PublishContext context)
    {
        var tableExists = await _ddlExecutor.TableExistsAsync(entity.DefaultTableName);
        if (!tableExists)
        {
            entity.Status = EntityStatus.Draft;
            await _db.SaveChangesAsync();
            return await PublishNewEntityInternalAsync(entity.Id, publishedBy, context);
        }

        var result = new PublishResult { EntityDefinitionId = entity.Id };
        try
        {
            await using var transaction = _db.Database.IsRelational()
                ? await _db.Database.BeginTransactionAsync()
                : null;
            entity.Status = EntityStatus.Published;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = publishedBy;
            if (context.IsCascadeChild)
            {
                entity.Source = EntitySource.System;
            }
            await _db.SaveChangesAsync();

            await _lockService.LockEntityAsync(entity.Id, "Entity republished from Withdrawn");
            if (transaction != null)
            {
                await transaction.CommitAsync();
            }
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    private async Task<bool> IsReferencedByPublishedEntitiesAsync(EntityDefinition entity)
    {
        var targetName = entity.EntityName?.Trim();
        if (string.IsNullOrWhiteSpace(targetName))
        {
            return false;
        }

        var targetLower = targetName.ToLowerInvariant();

        return await _db.FieldMetadatas
            .Where(f => !f.IsDeleted && f.LookupEntityName != null && f.LookupEntityName.ToLower() == targetLower)
            .Join(
                _db.EntityDefinitions.Where(ed => ed.Id != entity.Id && ed.Status == EntityStatus.Published),
                field => field.EntityDefinitionId,
                definition => definition.Id,
                (_, definition) => definition.Id)
            .AnyAsync();
    }

    private string ResolveWithdrawalMode()
    {
        var configured = _configuration["EntityPublishing:WithdrawalMode"]?.Trim();
        if (string.Equals(configured, WithdrawalModes.Physical, StringComparison.OrdinalIgnoreCase))
        {
            return WithdrawalModes.Physical;
        }

        return WithdrawalModes.Logical;
    }

    private static class WithdrawalModes
    {
        public const string Logical = "Logical";
        public const string Physical = "Physical";
    }

    /// <summary>
    /// 应用接口字段到实体
    /// </summary>
    private Task ApplyInterfaceFieldsAsync(EntityDefinition entity)
    {
        var existingFieldNames = entity.Fields.Select(f => f.PropertyName).ToHashSet();

        foreach (var entityInterface in entity.Interfaces.Where(i => i.IsEnabled))
        {
            var interfaceFields = _ddlGenerator.GenerateInterfaceFields(entityInterface);

            foreach (var field in interfaceFields)
            {
                // 避免重复添加
                if (!existingFieldNames.Contains(field.PropertyName))
                {
                    field.EntityDefinitionId = entity.Id;
                    entity.Fields.Add(field);
                    existingFieldNames.Add(field.PropertyName);
                }
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 分析实体变更
    /// </summary>
    private async Task<ChangeAnalysis> AnalyzeChangesAsync(EntityDefinition entity)
    {
        var analysis = new ChangeAnalysis();
        var tableName = entity.DefaultTableName;

        // 获取当前表结构
        var currentColumns = await _ddlExecutor.GetTableColumnsAsync(tableName);
        var currentColumnNames = currentColumns.Select(c => c.ColumnName.ToLower()).ToHashSet();

        // 分析新增字段
        foreach (var field in entity.Fields)
        {
            if (!currentColumnNames.Contains(field.PropertyName.ToLower()))
            {
                analysis.NewFields.Add(field);
            }
        }

        // 分析字段长度变更（简化实现）
        foreach (var field in entity.Fields)
        {
            var currentColumn = currentColumns.FirstOrDefault(c =>
                c.ColumnName.Equals(field.PropertyName, StringComparison.OrdinalIgnoreCase));

            if (currentColumn != null && field.Length.HasValue && currentColumn.MaxLength.HasValue)
            {
                if (field.Length > currentColumn.MaxLength)
                {
                    analysis.LengthIncreases[field] = field.Length.Value;
                }
                else if (field.Length < currentColumn.MaxLength)
                {
                    analysis.LengthDecreases[field] = field.Length.Value;
                    analysis.HasDestructiveChanges = true;
                }
            }
        }

        // 分析删除的字段
        var definedFieldNames = entity.Fields.Select(f => f.PropertyName.ToLower()).ToHashSet();
        foreach (var column in currentColumns)
        {
            if (!definedFieldNames.Contains(column.ColumnName.ToLower()))
            {
                analysis.RemovedFields.Add(column.ColumnName);
                analysis.HasDestructiveChanges = true;
            }
        }

        return analysis;
    }

    /// <summary>
    /// 生成ALTER脚本
    /// </summary>
    private string GenerateAlterScript(EntityDefinition entity, ChangeAnalysis analysis)
    {
        var scripts = new List<string>();

        // 添加新字段
        if (analysis.NewFields.Any())
        {
            var addScript = _ddlGenerator.GenerateAlterTableAddColumns(entity, analysis.NewFields);
            scripts.Add(addScript);
        }

        // 修改字段长度
        if (analysis.LengthIncreases.Any())
        {
            var modifyScript = _ddlGenerator.GenerateAlterTableModifyColumns(entity, analysis.LengthIncreases);
            scripts.Add(modifyScript);
        }

        return string.Join("\n", scripts);
    }

    private async Task GenerateTemplatesAndMenusAsync(EntityDefinition entity, string? publishedBy, PublishResult result)
    {
        var generatorResult = await _defaultTemplateService.EnsureTemplatesAsync(entity, publishedBy);

        foreach (var template in generatorResult.Templates)
        {
            result.Templates.Add(new PublishedTemplateInfo(template.Key, template.Value.Id, template.Value.Name));
        }

        var bindingMap = new Dictionary<string, TemplateStateBinding>();
        var codes = BuildFunctionCodes(entity.EntityRoute);
        var updatedBy = string.IsNullOrWhiteSpace(publishedBy) ? "system" : publishedBy!;

        foreach (var template in generatorResult.Templates)
        {
            var usage = MapViewStateToUsage(template.Key);
            var requiredCode = usage switch
            {
                FormTemplateUsageType.List => codes.ListCode,
                FormTemplateUsageType.Edit => codes.EditCode,
                FormTemplateUsageType.Combined => codes.EditCode,
                _ => codes.DetailCode
            };

            await _templateBindingService.UpsertBindingAsync(
                entity.EntityRoute,
                usage,
                template.Value.Id,
                true,
                updatedBy,
                requiredCode);
        }

        // List 视图
        if (generatorResult.Templates.TryGetValue("List", out var listTemplate))
        {
            var binding = await EnsureTemplateStateBindingAsync(
                entity.EntityRoute,
                "List",
                listTemplate.Id,
                codes.ListCode);

            result.TemplateBindings.Add(new PublishedTemplateBindingInfo(
                "List",
                FormTemplateUsageType.List,
                binding.Id,
                binding.TemplateId,
                binding.RequiredPermission ?? string.Empty));
            bindingMap["List"] = binding;
        }

        // DetailView 视图
        if (generatorResult.Templates.TryGetValue("DetailView", out var detailTemplate))
        {
            var binding = await EnsureTemplateStateBindingAsync(
                entity.EntityRoute,
                "DetailView",
                detailTemplate.Id,
                codes.DetailCode);

            result.TemplateBindings.Add(new PublishedTemplateBindingInfo(
                "DetailView",
                FormTemplateUsageType.Detail,
                binding.Id,
                binding.TemplateId,
                binding.RequiredPermission ?? string.Empty));
            bindingMap["DetailView"] = binding;
        }

        // DetailEdit 视图
        if (generatorResult.Templates.TryGetValue("DetailEdit", out var editTemplate))
        {
            var binding = await EnsureTemplateStateBindingAsync(
                entity.EntityRoute,
                "DetailEdit",
                editTemplate.Id,
                codes.EditCode);

            result.TemplateBindings.Add(new PublishedTemplateBindingInfo(
                "DetailEdit",
                FormTemplateUsageType.Edit,
                binding.Id,
                binding.TemplateId,
                binding.RequiredPermission ?? string.Empty));
            bindingMap["DetailEdit"] = binding;
        }

        // Create 视图
        if (generatorResult.Templates.TryGetValue("Create", out var createTemplate))
        {
            var binding = await EnsureTemplateStateBindingAsync(
                entity.EntityRoute,
                "Create",
                createTemplate.Id,
                codes.EditCode); // Create 使用和 Edit 相同的权限

            result.TemplateBindings.Add(new PublishedTemplateBindingInfo(
                "Create",
                FormTemplateUsageType.Combined,
                binding.Id,
                binding.TemplateId,
                binding.RequiredPermission ?? string.Empty));
            bindingMap["Create"] = binding;
        }

        if (bindingMap.Count > 0)
        {
            var nodes = await _accessService.EnsureEntityMenuAsync(entity, bindingMap);
            foreach (var node in nodes)
            {
                result.MenuNodes.Add(new PublishedMenuInfo(
                    node.Value.Code,
                    node.Value.Id,
                    node.Value.ParentId,
                    node.Value.Route,
                    node.Key,
                    MapViewStateToUsage(node.Key)));
            }
        }
    }

    /// <summary>
    /// 确保 TemplateStateBinding 存在
    /// </summary>
    private async Task<TemplateStateBinding> EnsureTemplateStateBindingAsync(
        string entityType,
        string viewState,
        int templateId,
        string? requiredPermission)
    {
        var binding = await _db.TemplateStateBindings
            .FirstOrDefaultAsync(b =>
                b.EntityType == entityType &&
                b.ViewState == viewState &&
                b.IsDefault);

        if (binding == null)
        {
            binding = new TemplateStateBinding
            {
                EntityType = entityType,
                ViewState = viewState,
                TemplateId = templateId,
                IsDefault = true,
                RequiredPermission = requiredPermission,
                CreatedAt = DateTime.UtcNow
            };
            _db.TemplateStateBindings.Add(binding);
        }
        else
        {
            binding.TemplateId = templateId;
            binding.RequiredPermission = requiredPermission;
        }

        await _db.SaveChangesAsync();
        return binding;
    }

    /// <summary>
    /// 验证实体中所有枚举字段的引用是否有效
    /// </summary>
    private async Task<EnumValidationResult> ValidateEnumReferencesAsync(EntityDefinition entity)
    {
        var result = new EnumValidationResult { IsValid = true };

        // 获取所有枚举字段
        var enumFields = entity.Fields
            .Where(f => f.DataType == FieldDataType.Enum && !f.IsDeleted)
            .ToList();

        if (enumFields.Count == 0)
        {
            return result; // 没有枚举字段，无需验证
        }

        // 加载所有枚举定义
        var enumIds = enumFields
            .Where(f => f.EnumDefinitionId.HasValue)
            .Select(f => f.EnumDefinitionId!.Value)
            .Distinct()
            .ToList();

        var existingEnums = await _db.EnumDefinitions
            .Where(e => enumIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e);

        // 验证每个枚举字段
        foreach (var field in enumFields)
        {
            if (!field.EnumDefinitionId.HasValue)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Field '{field.PropertyName}' has DataType=Enum but EnumDefinitionId is null. Please select an enum definition.";
                return result;
            }

            if (!existingEnums.TryGetValue(field.EnumDefinitionId.Value, out var enumDef))
            {
                result.IsValid = false;
                result.ErrorMessage = $"Field '{field.PropertyName}' references enum '{field.EnumDefinitionId.Value}' which does not exist. Please select a valid enum definition.";
                return result;
            }

            if (!enumDef.IsEnabled)
            {
                result.IsValid = false;
                var enumName = enumDef.DisplayName.TryGetValue("zh", out var name) ? name : enumDef.Code;
                result.ErrorMessage = $"Field '{field.PropertyName}' references enum '{enumName}' which is disabled. Please enable the enum or select a different one.";
                return result;
            }
        }

        _logger.LogInformation("[Publish] ✓ Enum reference validation passed for {Count} enum fields", enumFields.Count);
        return result;
    }

    private async Task<LookupValidationResult> ValidateLookupReferencesAsync(EntityDefinition entity)
    {
        var result = new LookupValidationResult { IsValid = true };

        var lookupFields = entity.Fields
            .Where(f => !f.IsDeleted && !string.IsNullOrWhiteSpace(f.LookupEntityName))
            .ToList();

        if (lookupFields.Count == 0)
        {
            return result;
        }

        foreach (var field in lookupFields)
        {
            if (field.ForeignKeyAction == ForeignKeyAction.SetNull &&
                field.IsRequiredExplicitlySet &&
                field.IsRequired)
            {
                result.IsValid = false;
                result.ErrorMessage =
                    $"Field '{field.PropertyName}' uses ForeignKeyAction=SetNull but is NOT NULL. Please set IsRequired=false or change ForeignKeyAction.";
                return result;
            }
        }

        var requestedEntityNames = lookupFields
            .Select(f => f.LookupEntityName!.Trim())
            .Where(name => !string.Equals(name, entity.EntityName, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (requestedEntityNames.Count == 0)
        {
            return result;
        }

        var existingEntityNames = await _db.EntityDefinitions
            .Where(e => requestedEntityNames.Contains(e.EntityName) && e.Status == EntityStatus.Published)
            .Select(e => e.EntityName)
            .ToListAsync();

        var existingSet = existingEntityNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = requestedEntityNames.Where(name => !existingSet.Contains(name)).ToList();
        if (missing.Count > 0)
        {
            result.IsValid = false;
            result.ErrorMessage = $"Lookup referenced entities not found or not published: {string.Join(", ", missing)}";
            return result;
        }

        _logger.LogInformation("[Publish] ? Lookup reference validation passed for {Count} lookup fields", lookupFields.Count);
        return result;
    }

    private sealed record PublishContext(HashSet<Guid> Visited, bool IsCascadeChild)
    {
        public static PublishContext CreateRoot() => new(new HashSet<Guid>(), false);

        public PublishContext AsCascadeChild() => this with { IsCascadeChild = true };
    }

    private static FormTemplateUsageType MapViewStateToUsage(string viewState) =>
        viewState switch
        {
            "List" => FormTemplateUsageType.List,
            "DetailEdit" => FormTemplateUsageType.Edit,
            "Create" => FormTemplateUsageType.Combined,
            _ => FormTemplateUsageType.Detail
        };

    private static (string ListCode, string DetailCode, string EditCode) BuildFunctionCodes(string entityRoute)
    {
        var baseCode = $"CRM.CORE.{entityRoute.ToUpperInvariant()}";
        return (
            baseCode,
            $"{baseCode}.DETAIL",
            $"{baseCode}.EDIT");
    }
}
