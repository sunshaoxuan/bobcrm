using BobCrm.Api.Abstractions;
using BobCrm.Api.Base;
using BobCrm.Api.Contracts.DTOs;
using BobCrm.Api.Core.DomainCommon;
using BobCrm.Api.Core.Persistence;
using Microsoft.Extensions.Logging;

namespace BobCrm.Api.Services;

/// <summary>
/// 模板管理服务实现
/// </summary>
public class TemplateService : ITemplateService
{
    private readonly IRepository<FormTemplate> _repo;
    private readonly IUnitOfWork _uow;
    private readonly II18nService _i18n;
    private readonly ILogger<TemplateService> _logger;

    public TemplateService(
        IRepository<FormTemplate> repo,
        IUnitOfWork uow,
        II18nService i18n,
        ILogger<TemplateService> logger)
    {
        _repo = repo;
        _uow = uow;
        _i18n = i18n;
        _logger = logger;
    }

    public async Task<object> GetTemplatesAsync(
        string userId,
        string? entityType = null,
        string? usageType = null,
        string? templateType = null,
        string? groupBy = null)
    {
        _logger.LogDebug("[TemplateService] Retrieving templates for user {UserId}, entityType: {EntityType}, usageType: {UsageType}, templateType: {TemplateType}",
            userId, entityType ?? "all", usageType ?? "all", templateType ?? "all");

        // Query both user templates and system templates
        var query = _repo.Query(t => t.UserId == userId || t.IsSystemDefault);

        // 按实体类型过滤
        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(t => t.EntityType == entityType);
        }

        // 按用途过滤
        if (!string.IsNullOrWhiteSpace(usageType) && Enum.TryParse<FormTemplateUsageType>(usageType, true, out var parsedUsageType))
        {
            query = query.Where(t => t.UsageType == parsedUsageType);
        }

        // 按模板类型过滤（system/user）
        if (!string.IsNullOrWhiteSpace(templateType))
        {
            if (templateType.Equals("system", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.IsSystemDefault);
            }
            else if (templateType.Equals("user", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(t => t.UserId == userId && !t.IsSystemDefault);
            }
        }

        var templates = await Task.FromResult(query
            .OrderByDescending(t => t.IsSystemDefault)
            .ThenByDescending(t => t.IsUserDefault)
            .ThenByDescending(t => t.UpdatedAt)
            .ToList());

        // 按分组方式组织数据
        if (groupBy == "entity")
        {
            var grouped = templates.GroupBy(t => t.EntityType ?? "未分类")
                .Select(g => new
                {
                    EntityType = g.Key,
                    Templates = g.Select(t => new
                    {
                        t.Id,
                        t.Name,
                        t.EntityType,
                        t.UsageType,
                        t.IsUserDefault,
                        t.IsSystemDefault,
                        t.Description,
                        t.CreatedAt,
                        t.UpdatedAt,
                        t.IsInUse
                    })
                });
            return grouped;
        }
        else if (groupBy == "user")
        {
            // 按用户分组（未来扩展，管理员可以看到所有用户的模板）
            var grouped = new[]
            {
                new
                {
                    UserId = userId,
                    Templates = templates.Select(t => new
                    {
                        t.Id,
                        t.Name,
                        t.EntityType,
                        t.UsageType,
                        t.IsUserDefault,
                        t.IsSystemDefault,
                        t.Description,
                        t.CreatedAt,
                        t.UpdatedAt,
                        t.IsInUse
                    })
                }
            };
            return grouped;
        }
        else
        {
            // 平铺列表
            var result = templates.Select(t => new
            {
                t.Id,
                t.Name,
                t.EntityType,
                t.UsageType,
                t.IsUserDefault,
                t.IsSystemDefault,
                t.Description,
                t.CreatedAt,
                t.UpdatedAt,
                t.IsInUse
            });
            return result;
        }
    }

    public async Task<FormTemplate?> GetTemplateByIdAsync(int templateId, string userId)
    {
        var template = await Task.FromResult(
            _repo.Query(t => t.Id == templateId && t.UserId == userId).FirstOrDefault());

        if (template == null)
        {
            _logger.LogWarning("[TemplateService] Template {TemplateId} not found for user {UserId}", templateId, userId);
        }

        return template;
    }

    public async Task<FormTemplate> CreateTemplateAsync(string userId, CreateTemplateRequest request)
    {
        _logger.LogInformation("[TemplateService] Creating template for user {UserId}, name: {Name}, entityType: {EntityType}",
            userId, request.Name, request.EntityType ?? "null");

        // 如果设置为用户默认模板，需要取消同一实体类型下的其他用户默认模板
        if (request.IsUserDefault && !string.IsNullOrWhiteSpace(request.EntityType))
        {
            await ClearExistingUserDefaultsAsync(userId, request.EntityType, null);
        }

        var template = new FormTemplate
        {
            Name = request.Name,
            EntityType = request.EntityType,
            UserId = userId,
            IsUserDefault = request.IsUserDefault,
            IsSystemDefault = false, // 只有管理员可以设置系统默认
            LayoutJson = request.LayoutJson,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsInUse = false
        };

        await _repo.AddAsync(template);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("[TemplateService] Template created successfully with ID {TemplateId}", template.Id);

        return template;
    }

    public async Task<FormTemplate> UpdateTemplateAsync(int templateId, string userId, UpdateTemplateRequest request)
    {
        var template = await Task.FromResult(
            _repo.Query(t => t.Id == templateId && t.UserId == userId).FirstOrDefault());

        if (template == null)
        {
            _logger.LogWarning("[TemplateService] Template {TemplateId} not found for user {UserId}", templateId, userId);
            throw new KeyNotFoundException(_i18n.T("MSG_TEMPLATE_NOT_FOUND"));
        }

        _logger.LogInformation("[TemplateService] Updating template {TemplateId} for user {UserId}", templateId, userId);

        // EntityType一旦设置后不允许修改
        if (!string.IsNullOrWhiteSpace(template.EntityType) &&
            request.EntityType != null &&
            request.EntityType != template.EntityType)
        {
            _logger.LogWarning("[TemplateService] Attempted to change EntityType from {Old} to {New}",
                template.EntityType, request.EntityType);
            throw new InvalidOperationException(_i18n.T("MSG_CANNOT_CHANGE_ENTITY_TYPE"));
        }

        // 如果设置为用户默认模板，需要取消同一实体类型下的其他用户默认模板
        if (request.IsUserDefault == true && !string.IsNullOrWhiteSpace(template.EntityType))
        {
            await ClearExistingUserDefaultsAsync(userId, template.EntityType, templateId);
        }

        // 更新字段
        if (request.Name != null) template.Name = request.Name;
        if (request.EntityType != null && string.IsNullOrWhiteSpace(template.EntityType))
        {
            template.EntityType = request.EntityType;
        }
        if (request.IsUserDefault != null) template.IsUserDefault = request.IsUserDefault.Value;
        if (request.LayoutJson != null) template.LayoutJson = request.LayoutJson;
        if (request.Description != null) template.Description = request.Description;

        template.UpdatedAt = DateTime.UtcNow;

        _repo.Update(template);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("[TemplateService] Template {TemplateId} updated successfully", templateId);

        return template;
    }

    public async Task DeleteTemplateAsync(int templateId, string userId)
    {
        var template = await Task.FromResult(
            _repo.Query(t => t.Id == templateId && t.UserId == userId).FirstOrDefault());

        if (template == null)
        {
            _logger.LogWarning("[TemplateService] Template {TemplateId} not found for user {UserId}", templateId, userId);
            throw new KeyNotFoundException(_i18n.T("MSG_TEMPLATE_NOT_FOUND"));
        }

        // 系统默认模板不允许删除
        if (template.IsSystemDefault)
        {
            _logger.LogWarning("[TemplateService] Attempted to delete system default template {TemplateId}", templateId);
            throw new InvalidOperationException(_i18n.T("MSG_CANNOT_DELETE_SYSTEM_TEMPLATE"));
        }

        // 用户默认模板不允许删除
        if (template.IsUserDefault)
        {
            _logger.LogWarning("[TemplateService] Attempted to delete user default template {TemplateId}", templateId);
            throw new InvalidOperationException(_i18n.T("MSG_CANNOT_DELETE_USER_DEFAULT"));
        }

        // 正在使用的模板不允许删除
        if (template.IsInUse)
        {
            _logger.LogWarning("[TemplateService] Attempted to delete in-use template {TemplateId}", templateId);
            throw new InvalidOperationException(_i18n.T("MSG_CANNOT_DELETE_IN_USE"));
        }

        _repo.Remove(template);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("[TemplateService] Template {TemplateId} deleted successfully", templateId);
    }

    public async Task<FormTemplate> CopyTemplateAsync(int sourceTemplateId, string userId, CopyTemplateRequest request)
    {
        // 查找源模板（可以是系统模板或用户模板）
        var sourceTemplate = await Task.FromResult(
            _repo.Query(t => t.Id == sourceTemplateId && (t.UserId == userId || t.IsSystemDefault))
                .FirstOrDefault());

        if (sourceTemplate == null)
        {
            _logger.LogWarning("[TemplateService] Template {TemplateId} not found for copying by user {UserId}", sourceTemplateId, userId);
            throw new KeyNotFoundException(_i18n.T("MSG_TEMPLATE_NOT_FOUND"));
        }

        _logger.LogInformation("[TemplateService] Copying template {TemplateId} for user {UserId}, new name: {Name}",
            sourceTemplateId, userId, request.Name);

        // 创建新模板（深拷贝）
        var newTemplate = new FormTemplate
        {
            Name = request.Name ?? $"{sourceTemplate.Name} (Copy)",
            EntityType = request.EntityType ?? sourceTemplate.EntityType,
            UserId = userId,
            IsUserDefault = false, // 复制的模板默认不是用户默认模板
            IsSystemDefault = false, // 用户创建的模板不能是系统模板
            LayoutJson = sourceTemplate.LayoutJson,
            UsageType = request.UsageType ?? sourceTemplate.UsageType,
            Description = request.Description ?? $"从 '{sourceTemplate.Name}' 复制",
            Tags = sourceTemplate.Tags != null ? new List<string>(sourceTemplate.Tags) : null,
            RequiredFunctionCode = sourceTemplate.RequiredFunctionCode,
            LayoutMode = sourceTemplate.LayoutMode,
            DetailDisplayMode = sourceTemplate.DetailDisplayMode,
            DetailRoute = sourceTemplate.DetailRoute,
            ModalSize = sourceTemplate.ModalSize,
            Version = 1, // 新模板版本从 1 开始
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsInUse = false
        };

        await _repo.AddAsync(newTemplate);
        await _uow.SaveChangesAsync();

        _logger.LogInformation("[TemplateService] Template copied successfully, new ID: {TemplateId}", newTemplate.Id);

        return newTemplate;
    }

    public async Task<FormTemplate> ApplyTemplateAsync(int templateId, string userId)
    {
        // 查找要应用的模板
        var template = await Task.FromResult(
            _repo.Query(t => t.Id == templateId && (t.UserId == userId || t.IsSystemDefault))
                .FirstOrDefault());

        if (template == null)
        {
            _logger.LogWarning("[TemplateService] Template {TemplateId} not found for applying by user {UserId}", templateId, userId);
            throw new KeyNotFoundException(_i18n.T("MSG_TEMPLATE_NOT_FOUND"));
        }

        if (string.IsNullOrWhiteSpace(template.EntityType))
        {
            _logger.LogWarning("[TemplateService] Cannot apply template {TemplateId} without EntityType", templateId);
            throw new InvalidOperationException(_i18n.T("MSG_TEMPLATE_NO_ENTITY_TYPE"));
        }

        _logger.LogInformation("[TemplateService] Applying template {TemplateId} for user {UserId}, entity: {EntityType}",
            templateId, userId, template.EntityType);

        // 如果是系统模板，需要先复制一份给用户
        if (template.IsSystemDefault)
        {
            // 检查用户是否已经有这个模板的副本
            var existingCopy = await Task.FromResult(
                _repo.Query(t => t.UserId == userId &&
                               t.EntityType == template.EntityType &&
                               t.UsageType == template.UsageType &&
                               t.Name == template.Name)
                    .FirstOrDefault());

            if (existingCopy != null)
            {
                // 使用现有副本
                template = existingCopy;
            }
            else
            {
                // 创建新副本
                var copy = new FormTemplate
                {
                    Name = template.Name,
                    EntityType = template.EntityType,
                    UserId = userId,
                    IsUserDefault = false,
                    IsSystemDefault = false,
                    LayoutJson = template.LayoutJson,
                    UsageType = template.UsageType,
                    Description = $"从系统模板 '{template.Name}' 复制",
                    Tags = template.Tags != null ? new List<string>(template.Tags) : null,
                    RequiredFunctionCode = template.RequiredFunctionCode,
                    LayoutMode = template.LayoutMode,
                    DetailDisplayMode = template.DetailDisplayMode,
                    DetailRoute = template.DetailRoute,
                    ModalSize = template.ModalSize,
                    Version = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsInUse = false
                };

                await _repo.AddAsync(copy);
                template = copy;
            }
        }

        // 取消同一实体类型和用途下的其他用户默认模板
        var existingDefaults = _repo.Query(t => t.UserId == userId &&
            t.EntityType == template.EntityType &&
            t.UsageType == template.UsageType &&
            t.IsUserDefault &&
            t.Id != template.Id).ToList();

        foreach (var existing in existingDefaults)
        {
            existing.IsUserDefault = false;
            _repo.Update(existing);
            _logger.LogDebug("[TemplateService] Cleared user default flag from template {TemplateId}", existing.Id);
        }

        // 设置为用户默认模板
        template.IsUserDefault = true;
        template.UpdatedAt = DateTime.UtcNow;
        _repo.Update(template);

        await _uow.SaveChangesAsync();

        _logger.LogInformation("[TemplateService] Template {TemplateId} applied successfully as user default", template.Id);

        return template;
    }

    public async Task<FormTemplate?> GetEffectiveTemplateAsync(string entityType, string userId)
    {
        _logger.LogDebug("[TemplateService] Getting effective template for entity {EntityType}, user {UserId}",
            entityType, userId);

        // 1. 优先查找用户默认模板
        var userDefault = await Task.FromResult(_repo.Query(t =>
            t.UserId == userId &&
            t.EntityType == entityType &&
            t.IsUserDefault).FirstOrDefault());

        if (userDefault != null)
        {
            _logger.LogDebug("[TemplateService] Found user default template {TemplateId}", userDefault.Id);
            return userDefault;
        }

        // 2. 查找系统默认模板
        var systemDefault = await Task.FromResult(_repo.Query(t =>
            t.EntityType == entityType &&
            t.IsSystemDefault).FirstOrDefault());

        if (systemDefault != null)
        {
            _logger.LogDebug("[TemplateService] Found system default template {TemplateId}", systemDefault.Id);
            return systemDefault;
        }

        // 3. 查找该实体类型的第一个模板（任意用户）
        var firstTemplate = await Task.FromResult(_repo.Query(t => t.EntityType == entityType)
            .OrderBy(t => t.CreatedAt)
            .FirstOrDefault());

        if (firstTemplate != null)
        {
            _logger.LogDebug("[TemplateService] Found first template {TemplateId} for entity type", firstTemplate.Id);
            return firstTemplate;
        }

        _logger.LogDebug("[TemplateService] No template found for entity {EntityType}", entityType);
        return null;
    }

    /// <summary>
    /// 清除同一实体类型下的其他用户默认模板
    /// </summary>
    private async Task ClearExistingUserDefaultsAsync(string userId, string entityType, int? excludeTemplateId)
    {
        var existingDefaults = _repo.Query(t => t.UserId == userId &&
            t.EntityType == entityType &&
            t.IsUserDefault).ToList();

        if (excludeTemplateId.HasValue)
        {
            existingDefaults = existingDefaults.Where(t => t.Id != excludeTemplateId.Value).ToList();
        }

        foreach (var existing in existingDefaults)
        {
            existing.IsUserDefault = false;
            _repo.Update(existing);
        }

        if (existingDefaults.Any())
        {
            _logger.LogInformation("[TemplateService] Cleared {Count} existing user default templates", existingDefaults.Count);
            await Task.CompletedTask;
        }
    }
}
