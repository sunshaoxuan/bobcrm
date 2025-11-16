using System;
using System.Collections.Generic;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;
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

    public EntityPublishingService(
        AppDbContext db,
        PostgreSQLDDLGenerator ddlGenerator,
        DDLExecutionService ddlExecutor,
        IEntityLockService lockService,
        TemplateBindingService templateBindingService,
        AccessService accessService,
        IDefaultTemplateService defaultTemplateService,
        ILogger<EntityPublishingService> logger)
    {
        _db = db;
        _ddlGenerator = ddlGenerator;
        _ddlExecutor = ddlExecutor;
        _lockService = lockService;
        _templateBindingService = templateBindingService;
        _accessService = accessService;
        _defaultTemplateService = defaultTemplateService;
        _logger = logger;
    }

    /// <summary>
    /// 发布新实体（CREATE TABLE）
    /// </summary>
    public async Task<PublishResult> PublishNewEntityAsync(Guid entityDefinitionId, string? publishedBy = null)
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

            // 5. 生成CREATE TABLE DDL
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

            // 7. 更新实体状态为Published
            entity.Status = EntityStatus.Published;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = publishedBy;
            await _db.SaveChangesAsync();

            // 8. 锁定实体定义（防止发布后误修改关键属性）
            await _lockService.LockEntityAsync(entityDefinitionId, "Entity published");

            await GenerateTemplatesAndMenusAsync(entity, publishedBy, result);

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

            // 3. 检查表是否存在
            var tableName = entity.DefaultTableName;
            var tableExists = await _ddlExecutor.TableExistsAsync(tableName);
            if (!tableExists)
            {
                result.Success = false;
                result.ErrorMessage = $"Table '{tableName}' does not exist. Use PublishNewEntity instead.";
                return result;
            }

            // 4. 分析变更
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

            // 8. 更新实体状态为Published
            entity.Status = EntityStatus.Published;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.UpdatedBy = publishedBy;
            await _db.SaveChangesAsync();

            await GenerateTemplatesAndMenusAsync(entity, publishedBy, result);

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

    /// <summary>
    /// 应用接口字段到实体
    /// </summary>
    private async Task ApplyInterfaceFieldsAsync(EntityDefinition entity)
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

        await _db.SaveChangesAsync();
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

        var bindingMap = new Dictionary<FormTemplateUsageType, TemplateBinding>();
        var codes = BuildFunctionCodes(entity.EntityRoute);
        var updatedBy = string.IsNullOrWhiteSpace(publishedBy) ? "system" : publishedBy!;

        if (generatorResult.Templates.TryGetValue(FormTemplateUsageType.Detail, out var detailTemplate))
        {
            var binding = await _templateBindingService.UpsertBindingAsync(
                entity.EntityRoute,
                FormTemplateUsageType.Detail,
                detailTemplate.Id,
                isSystem: true,
                updatedBy,
                codes.DetailCode);

            result.TemplateBindings.Add(new PublishedTemplateBindingInfo(
                FormTemplateUsageType.Detail,
                binding.Id,
                binding.TemplateId,
                binding.RequiredFunctionCode ?? string.Empty));
            bindingMap[FormTemplateUsageType.Detail] = binding;
        }

        if (generatorResult.Templates.TryGetValue(FormTemplateUsageType.Edit, out var editTemplate))
        {
            var binding = await _templateBindingService.UpsertBindingAsync(
                entity.EntityRoute,
                FormTemplateUsageType.Edit,
                editTemplate.Id,
                isSystem: true,
                updatedBy,
                codes.EditCode);

            result.TemplateBindings.Add(new PublishedTemplateBindingInfo(
                FormTemplateUsageType.Edit,
                binding.Id,
                binding.TemplateId,
                binding.RequiredFunctionCode ?? string.Empty));
            bindingMap[FormTemplateUsageType.Edit] = binding;
        }

        if (generatorResult.Templates.TryGetValue(FormTemplateUsageType.List, out var listTemplate))
        {
            var binding = await _templateBindingService.UpsertBindingAsync(
                entity.EntityRoute,
                FormTemplateUsageType.List,
                listTemplate.Id,
                isSystem: true,
                updatedBy,
                codes.ListCode);

            result.TemplateBindings.Add(new PublishedTemplateBindingInfo(
                FormTemplateUsageType.List,
                binding.Id,
                binding.TemplateId,
                binding.RequiredFunctionCode ?? string.Empty));
            bindingMap[FormTemplateUsageType.List] = binding;
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
                    node.Key));
            }
        }
    }

    private static (string ListCode, string DetailCode, string EditCode) BuildFunctionCodes(string entityRoute)
    {
        var baseCode = $"CRM.CORE.{entityRoute.ToUpperInvariant()}";
        return (
            baseCode,
            $"{baseCode}.DETAIL",
            $"{baseCode}.EDIT");
    }
}

/// <summary>
/// 发布结果
/// </summary>
public class PublishResult
{
    public Guid EntityDefinitionId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DDLScript { get; set; }
    public Guid ScriptId { get; set; }
    public ChangeAnalysis? ChangeAnalysis { get; set; }
    public List<PublishedTemplateInfo> Templates { get; } = new();
    public List<PublishedTemplateBindingInfo> TemplateBindings { get; } = new();
    public List<PublishedMenuInfo> MenuNodes { get; } = new();
}

/// <summary>
/// 变更分析结果
/// </summary>
public class ChangeAnalysis
{
    public List<FieldMetadata> NewFields { get; set; } = new();
    public Dictionary<FieldMetadata, int> LengthIncreases { get; set; } = new();
    public Dictionary<FieldMetadata, int> LengthDecreases { get; set; } = new();
    public List<string> RemovedFields { get; set; } = new();
    public bool HasDestructiveChanges { get; set; }
}

public record PublishedTemplateInfo(FormTemplateUsageType UsageType, int TemplateId, string TemplateName);

public record PublishedTemplateBindingInfo(FormTemplateUsageType UsageType, int BindingId, int TemplateId, string RequiredFunctionCode);

public record PublishedMenuInfo(string Code, Guid NodeId, Guid? ParentId, string? Route, FormTemplateUsageType UsageType);
