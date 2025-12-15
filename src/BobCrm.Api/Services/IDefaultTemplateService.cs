using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;
using BobCrm.Application.Templates;

namespace BobCrm.Api.Services;

/// <summary>
/// 默认模板服务接口
/// </summary>
public interface IDefaultTemplateService
{
    /// <summary>
    /// 确保实体的所有默认模板存在
    /// </summary>
    Task<DefaultTemplateGenerationResult> EnsureTemplatesAsync(
        EntityDefinition entityDefinition,
        string? updatedBy,
        bool force = false,
        CancellationToken ct = default);

    /// <summary>
    /// 获取实体的默认模板
    /// </summary>
    /// <param name="entityDefinition">实体定义</param>
    /// <param name="usageType">模板用途</param>
    /// <param name="requestedBy">请求人</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>默认模板</returns>
    Task<FormTemplate> GetDefaultTemplateAsync(
        EntityDefinition entityDefinition,
        FormTemplateUsageType usageType,
        string? requestedBy = null,
        CancellationToken ct = default);
}

