using System.Collections.Generic;
using BobCrm.Api.Base;

namespace BobCrm.Application.Templates;

/// <summary>
/// 默认模板生成结果
/// </summary>
public class DefaultTemplateGenerationResult
{
    /// <summary>
    /// 按视图状态分组的模板字典
    /// </summary>
    public Dictionary<string, FormTemplate> Templates { get; } = new();

    /// <summary>
    /// 新创建的模板列表
    /// </summary>
    public List<FormTemplate> Created { get; } = new();

    /// <summary>
    /// 更新的模板列表
    /// </summary>
    public List<FormTemplate> Updated { get; } = new();
}
