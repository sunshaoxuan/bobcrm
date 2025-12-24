using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;

namespace BobCrm.Application.Templates;

/// <summary>
/// 默认模板生成器接口
/// </summary>
public interface IDefaultTemplateGenerator
{
    /// <summary>
    /// 为指定实体和视图状态生成模板
    /// </summary>
    /// <param name="entityDefinition">实体定义</param>
    /// <param name="usageType">模板用途</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成的模板</returns>
    Task<FormTemplate> GenerateAsync(
        EntityDefinition entityDefinition,
        FormTemplateUsageType usageType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 为指定实体和视图状态生成模板（向后兼容的重载）
    /// </summary>
    /// <param name="entityDefinition">实体定义</param>
    /// <param name="viewState">视图状态 (List, DetailView, DetailEdit, Create)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成的模板</returns>
    Task<FormTemplate> GenerateAsync(
        EntityDefinition entityDefinition,
        string viewState = "DetailView",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 确保实体的所有默认模板存在，不存在则创建
    /// </summary>
    /// <param name="entityDefinition">实体定义</param>
    /// <param name="force">强制重新生成</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>生成结果</returns>
    Task<DefaultTemplateGenerationResult> EnsureTemplatesAsync(
        EntityDefinition entityDefinition,
        bool force = false,
        CancellationToken cancellationToken = default);
}
