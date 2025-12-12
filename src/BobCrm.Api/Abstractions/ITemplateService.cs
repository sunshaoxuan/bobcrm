using BobCrm.Api.Base;
using BobCrm.Api.Contracts.DTOs.DataSet;
using BobCrm.Api.Contracts.DTOs.Template;
using BobCrm.Api.Contracts.Requests.Template;

namespace BobCrm.Api.Abstractions;

/// <summary>
/// 模板管理服务接口
/// </summary>
public interface ITemplateService
{
    /// <summary>
    /// 获取用户的模板列表
    /// </summary>
    Task<object> GetTemplatesAsync(
        string userId,
        string? entityType = null,
        string? usageType = null,
        string? templateType = null,
        string? groupBy = null);

    /// <summary>
    /// 获取单个模板详情
    /// </summary>
    Task<FormTemplate?> GetTemplateByIdAsync(int templateId, string userId);

    /// <summary>
    /// 创建新模板
    /// </summary>
    Task<FormTemplate> CreateTemplateAsync(string userId, CreateTemplateRequest request);

    /// <summary>
    /// 更新模板
    /// </summary>
    Task<FormTemplate> UpdateTemplateAsync(int templateId, string userId, UpdateTemplateRequest request);

    /// <summary>
    /// 删除模板
    /// </summary>
    Task DeleteTemplateAsync(int templateId, string userId);

    /// <summary>
    /// 复制模板
    /// </summary>
    Task<FormTemplate> CopyTemplateAsync(int sourceTemplateId, string userId, CopyTemplateRequest request);

    /// <summary>
    /// 应用模板（设置为用户默认模板）
    /// </summary>
    Task<FormTemplate> ApplyTemplateAsync(int templateId, string userId);

    /// <summary>
    /// 获取有效模板（按优先级：用户默认 > 系统默认 > 第一个创建的模板）
    /// </summary>
    Task<FormTemplate?> GetEffectiveTemplateAsync(string entityType, string userId);
}
