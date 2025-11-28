using System.Collections.Generic;
using BobCrm.Api.Base;
using BobCrm.Api.Base.Models;

namespace BobCrm.Api.Contracts.DTOs;

/// <summary>
/// 模板 DTO 扩展方法（已废弃 - 仅保留用于向后兼容）
/// </summary>
public static class TemplateDtoExtensions
{
    /// <summary>
    /// 将旧的 TemplateBinding 转换为 DTO（已废弃）
    /// </summary>
    [System.Obsolete("TemplateBinding is deprecated. Use TemplateStateBinding instead.")]
    public static TemplateBindingDto ToDto(this TemplateBinding binding) =>
        new(
            binding.Id,
            binding.EntityType,
            binding.UsageType,
            binding.TemplateId,
            binding.IsSystem,
            binding.RequiredFunctionCode,
            binding.UpdatedBy,
            binding.UpdatedAt);

    /// <summary>
    /// 将 FormTemplate 转换为描述符 DTO
    /// </summary>
    /// <param name="template">模板</param>
    /// <param name="usageType">用途类型（可覆盖模板自身的值）</param>
    /// <returns>模板描述符</returns>
    public static TemplateDescriptorDto ToDescriptor(this FormTemplate template, FormTemplateUsageType? usageType = null) =>
        new(
            template.Id,
            template.Name,
            template.EntityType,
            usageType ?? template.UsageType,
            template.LayoutJson,
            template.Tags ?? new List<string>(),
            template.Description);
}
